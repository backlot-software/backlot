using System.Text.RegularExpressions;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Raven.Client.Documents.Indexes;

namespace Backlot.Services.RavenDb;

// ReSharper disable once InconsistentNaming : RavenDb naming convention.
public class Roles_BySkillAndReadPermission : AbstractIndexCreationTask<IPersist>
{
    // ReSharper disable once InconsistentNaming : different naming convention.
    public static string _IndexName => "Roles/BySkillAndReadPermission";
    public override string IndexName => _IndexName;

    /// <summary>
    /// Suffix of the ngram (substring searchable) twin of every string dynamic field.
    /// Shared with RavenPersistedRoleRepository so the index and the query can not drift apart.
    /// It is deliberately a character that RavenPersistedRoleRepository.Rgx strips from a criteria
    /// fieldname, so user supplied criteria can never address a twin field directly.
    /// </summary>
    internal const string NGramSuffix = "~";
    
    private static int? _nGramMinGram;

    internal static int NGramMinGram
    {
        get
        {
            if (_nGramMinGram != null) return _nGramMinGram.Value;
            
            var min = ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.NGramMinGram);
            _nGramMinGram = !string.IsNullOrWhiteSpace(min) ? int.Parse(min) : 3;

            return _nGramMinGram.Value;
        }
    }
    
    private static int? _nGramMaxGram;

    internal static int NGramMaxGram
    {
        get
        {
            if (_nGramMaxGram != null) return _nGramMaxGram.Value;
            
            var max = ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.NGramMaxGram);
            _nGramMaxGram = !string.IsNullOrWhiteSpace(max) ? int.Parse(max) : 6;

            return _nGramMaxGram.Value;
        }
    }

    public Roles_BySkillAndReadPermission()
    {
        // NGramAnalyzer is bundled with the RavenDb server and is resolved by name.
        // Scoping it through the index Configuration (instead of a per field or __all_fields analyzer)
        // makes it apply to FieldIndexing.Search fields ONLY. Skills, CanRead, UsersCanRead,
        // GroupsCanRead and LastModified are not Search fields, so their exact term matching
        // (WhereEquals / ContainsAny / range) keeps working unchanged.
        Configuration["Indexing.Analyzers.Search.Default"] = "NGramAnalyzer";
        Configuration["Indexing.Analyzers.NGram.MinGram"] = NGramMinGram.ToString();
        Configuration["Indexing.Analyzers.NGram.MaxGram"] = NGramMaxGram.ToString();

        Map = roles => from role in roles
            let metadata = MetadataFor(role)
            let jsn = AsJson(role)
            select new
            {
                Skills = jsn[Meta.__Skills],
                LastModified = (DateTime)metadata[Db.LastModified], // name IS/(AND HAS TO BE) the same as IPersist.LastModified!
                CanRead = Regex.IsMatch(metadata[Db.Pcl].ToString(), "(?:m::[4-7]{1})"),
                // using a match and than select on groups does result in errors while indexing in ravendb. the alternative is to use a replace the smart way, the downside is a larger less optimized Regex.
                // explain below regex: first replace all , than replace all users with a level higher than 3 and finally replace all other users and groups. because only <values> with a permission level of 4-7 are catched in a captured group $1 only returns these values.
                // * is an accepted characters for the "g" group only, thats why it is not in the ...:[u]:)([A-z@\._-]+:) part.
                UsersCanRead = Regex.Replace(metadata[Db.Pcl].ToString(),
                        @",|(?:[u]:)([A-z@\._-]+:)(?:[4-7])|(?:[ugm]:)(?:[*A-z@\._-]*)(?::[0-7])",
                        "$1") // Avoid backtracking not supported yet? RavenDb 5.2: RegexOptions.NonBacktracking)
                    .Split(':', StringSplitOptions.RemoveEmptyEntries),
                GroupsCanRead = Regex.Replace(metadata[Db.Pcl].ToString(),
                        @",|(?:[g]:)([*A-z@\._-]+:)(?:[4-7])|(?:[ugm]:)(?:[*A-z@\._-]*)(?::[0-7])",
                        "$1") // Avoiding backtracking not supported yet? RavenDb 5.2 RegexOptions.NonBacktracking)
                    .Split(':', StringSplitOptions.RemoveEmptyEntries),
                // see: https://ravendb.net/docs/article-page/6.0/csharp/indexes/using-dynamic-fields#createfield-syntax
                // every scalar gets an exact field "_<Key>" for eq / lt / gt, and every string
                // additionally gets an analyzed twin "_<Key>~" for ct (Search).
                _ = jsn
                    // check if value is a number or a string. without using backlot libarry
                    .Where(kvp => kvp.Value is Boolean
                                  || kvp.Value is Int16 || kvp.Value is Int32 || kvp.Value is Int64
                                  || kvp.Value is Decimal || kvp.Value is Double
                                  || kvp.Value is IConvertible) // check if value is a number or a string. IConvertible is used by backlot server for sparrow types such as strings.
                    .Select(kvp => CreateField($"_{kvp.Key}", kvp.Value, new CreateFieldOptions
                    {
                        // see: https://ravendb.net/docs/article-page/6.0/csharp/indexes/storing-data-in-index seve disk space
                        Storage = FieldStorage.No,
                        // NOT analyzed: the whole value as a single lowercased term, so WhereEquals
                        // stays an equality and numbers keep their range (lt / gt) behaviour.
                        Indexing = FieldIndexing.Default
                    }))
                    .Concat(jsn
                        // strings only, and the TypeCode is what reliably separates them: every
                        // numeric type and Boolean is an IConvertible too, and a json floating point
                        // number arrives as a sparrow LazyNumberValue, which is a reference type
                        // (TypeCode.Object) so an "is ValueType" test would let it through here.
                        // do not rewrite this as !(kvp.Value is ValueType) either: ravendb serializes
                        // the map to c# source and drops the parenthesis, which no longer compiles.
                        .Where(kvp => Convert.GetTypeCode(kvp.Value) == TypeCode.String)
                        .Select(kvp => CreateField($"_{kvp.Key}{NGramSuffix}", kvp.Value, new CreateFieldOptions
                        {
                            Storage = FieldStorage.No,
                            // analyzed with Indexing.Analyzers.Search.Default == NGramAnalyzer, which
                            // turns Search into a substring match: 'jo' hits john, lenjonas and peter jo.
                            Indexing = FieldIndexing.Search
                        })))
            };	
    }
}
