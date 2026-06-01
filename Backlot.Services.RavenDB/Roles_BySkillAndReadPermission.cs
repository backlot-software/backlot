using System.Text.RegularExpressions;
using Backlot.Core;
using Backlot.Core.Json;
using Raven.Client.Documents.Indexes;

namespace Backlot.Services.RavenDb;

// ReSharper disable once InconsistentNaming : RavenDb naming convention.
public class Roles_BySkillAndReadPermission : AbstractIndexCreationTask<IPersist>
{
    // ReSharper disable once InconsistentNaming : different naming convention.
    public static string _IndexName => "Roles/BySkillAndReadPermission";
    public override string IndexName => _IndexName;

    public Roles_BySkillAndReadPermission()
    {
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
                _ = jsn
                    // check if value is a number or a string. without using backlot libarry
                    .Where(kvp => kvp.Value is Boolean
                                  || kvp.Value is Int16 || kvp.Value is Int32 || kvp.Value is Int64
                                  || kvp.Value is Decimal || kvp.Value is Double
                                  || kvp.Value is IConvertible) // check if value is a number or a string. IConvertible is used by backlot server for sparrow types such as strings.
                    // see: https://ravendb.net/docs/article-page/6.0/csharp/indexes/using-dynamic-fields#createfield-syntax
                    .Select(kvp => CreateField($"_{kvp.Key}", kvp.Value, 
                        false, // see: https://ravendb.net/docs/article-page/6.0/csharp/indexes/storing-data-in-index seve disk space
                        kvp.Value is IConvertible)) // FieldIndexing.Search for all strings but Exact for numbers.
            };	
    }
}