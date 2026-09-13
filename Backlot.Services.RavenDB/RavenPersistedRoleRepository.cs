using System.Text;
using System.Text.RegularExpressions;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;
using Sparrow.Json;
// ReSharper disable ConvertToUsingDeclaration

namespace Backlot.Services.RavenDb
{
    /// <summary>
    /// Persist roles into ravendb
    /// </summary>
    public sealed class RavenPersistedRoleRepository : BasePersistedRoleRepository, IDisposable
    {
        private readonly RavenUnitOfWork _uow;
        
        public RavenPersistedRoleRepository(IUnitOfWork unitOfWork)
        {
            _uow = unitOfWork as RavenUnitOfWork;
        }
        
        private const string Rgx =  @"[^A-Za-z0-9_\-.@]";

        /// <summary>
        /// Rewrites a "contains" search term into terms the ngram twin of a field can actually match.
        /// </summary>
        /// <remarks>
        /// NGramAnalyzer is only used to analyze the indexed value, never the search term, so the term
        /// stays a single token and a word longer than NGramMaxGram matches no indexed ngram at all.
        /// Ngrams never cross a word boundary either, so every word is cut on its own into overlapping
        /// pieces of at most NGramMaxGram characters, which the caller then requires all of (And).
        /// Words shorter than NGramMinGram have no ngram to match and are dropped.
        /// </remarks>
        /// <returns>The terms to search for, or null when the term has nothing matchable in it.</returns>
        internal static string ToNGramTerms(string value)
        {
            // split on everything the indexing tokenizer also treats as a separator, so that a term
            // like "roles/lenjonas" is cut per word instead of straddling the '/'.
            var words = Regex.Split(value ?? string.Empty, @"[^A-Za-z0-9]+");
            var terms = new List<string>();

            foreach (var word in words)
            {
                if (word.Length < Roles_BySkillAndReadPermission.NGramMinGram)
                    continue; // no indexed ngram is this short.

                if (word.Length <= Roles_BySkillAndReadPermission.NGramMaxGram)
                {
                    terms.Add(word);
                    continue;
                }

                // too long to be an indexed ngram, so slide a NGramMaxGram sized window over the word.
                for (var i = 0; i + Roles_BySkillAndReadPermission.NGramMaxGram <= word.Length; i++)
                {
                    terms.Add(word.Substring(i, Roles_BySkillAndReadPermission.NGramMaxGram));
                }
            }

            return terms.Count == 0 ? null : string.Join(' ', terms);
        }

        private static async Task<BlittableJsonReaderObject> ParseJson(JsonOperationContext context, string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return await context.ReadForMemoryAsync(stream, "json");
        }

        public override IEnumerable<Revision> GetRevisions<T>(string uid)
        {
            using (var session = Db.Store.OpenSession())
            {
                var persistedRevisions = session.Advanced.Revisions.GetFor<object>(uid)
                    .OfType<JObject>()
                    .ToList();

                if (!persistedRevisions.Any() && TryGet<T>(uid, out var current))
                {
                    return
                    [
                        new Revision
                        {
                            Checksum = current.GetChecksum(),
                            Content = current,
                            Reference = current.GetReference()
                        }
                    ];
                }

                return persistedRevisions.Select(revision =>
                {
                    var metadata = session.Advanced.GetMetadataFor(revision);
                    var role = revision.Presents<T>((persist, o) => (T)MetaDataInitializer.Initialize(persist, o, metadata));
                    return new Revision
                    {
                        Checksum = metadata[Db.Checksum]?.ToString(),
                        Reference = role.GetReference(),
                        Content = role
                    };
                }).ToList();
            }
        }

        public override bool TryGetPermission(string uid, out Permission permission)
        {
            using (var session = Db.Store.OpenSession())
            {
                // Load the document by ID
                var document = session.Load<dynamic>(uid);

                if (document != null)
                {
                    // Access the metadata
                    var metadata = session.Advanced.GetMetadataFor(document);
                    // Retrieve the __Permission value from the metadata
                    var p = metadata["__Permission"].ToString() as string;
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        permission = Permission.Deserialize(p);
                        return true;
                    }
                }
            }

            permission = Permission.Create(PermissionLevel.None);
            return false;
        }
        
        protected override async Task<TRole> Store<TRole>(TRole obj)
        {
            if(string.IsNullOrEmpty(obj.Uid))
                throw new ArgumentNullException($"Uid is null or empty for  {obj.GetType().GetRoleName()} with name '{obj.Name}', and therefor we can not persist this item.");
            
            #region proxied roles

            // ReSharper disable once SuspiciousTypeConversion.Global : is a dynamicproxy.
            if (obj is IProxiedRole)
            {
                //example from: https://stackoverflow.com/questions/49423637/ravendb-4-0-storing-raw-json

                //var backlotJson = Json.ToJson(obj, () => Db.Serializer);
                var ravObj = JObject.FromObject(obj, Db.Serializer);

                if (!ravObj.ContainsKey(Db.MetaData))
                {
                    ravObj.Add(new JProperty(Db.MetaData,
                            new JObject(
                                new JProperty(Db.Collecion, Db.RoleCollectionName),
                                new JProperty(Db.Checksum, obj.GetChecksum()),
                                new JProperty(Db.Pcl, obj.Permission().ToString())
                            )
                        )
                    );
                }

                var blittableJson = await ParseJson(_uow.AsyncSession.Advanced.Context, ravObj?.ToString());

                var command = new PutDocumentCommand(Db.Store.Conventions, obj.Uid, null, blittableJson);

                _uow.Commands[obj.Uid] = command;

                //return
                obj.LastModified = DateTimeOffset.Now;
                return obj;
            }

            #endregion

            #region self roles

            await _uow.AsyncSession.StoreAsync(obj);
            var metadata = _uow.AsyncSession.Advanced.GetMetadataFor(obj);
            metadata[Db.Checksum] = obj.GetChecksum();
            metadata[Db.Pcl] = obj.Permission().ToString();

            obj.LastModified = DateTimeOffset.Now;
            //return
            //GetMetaData(obj, obj);
            return obj;

            #endregion
        }
        
        protected override bool TryGetType(string uid, Type objType, out IRole obj)
        {
            using (var session = Db.Store.OpenSession())
            {
                var ravenObj = session.Load<object>(uid);

                obj = null;
                if (ravenObj == null) return false;

                var metadata = session.Advanced.GetMetadataFor(ravenObj);
                obj = ravenObj.PresentsType(objType, 
                    (r, o) => MetaDataInitializer.Initialize(r as IPersist, o, metadata));

                var result = obj != null;

                return result;
            }
        }

        public override void Terminate(string key)
        {
            using (var session = Db.Store.OpenSession())
            {
                session.Delete(key);
                session.SaveChanges();
            }
        }

        public override void FlushDb()
        {
            using(var session = Db.Store.OpenSession())
            {
                var roles = session.Advanced.RawQuery<object>($"from {Db.RoleCollectionName}").ToList();
                foreach (var obj in roles)
                {
                    session.Delete(obj);
                }

                var relations = session.Query<Relation>().ToList();
                foreach (var obj in relations)
                {
                    session.Delete(obj);
                }

                session.SaveChanges();
            }
        }

        public override IEnumerable<IRole> GetAll(Type objType,
            int page,
            int pageSize,
            out int total,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null)
        {
            var result = Query(objType, page, pageSize, out var stats, criteria, from, till, orderby);
            total = Convert.ToInt32(stats.TotalResults);
            return result;
        }

        public override IEnumerable<T> GetAll<T>(int page,
            int pageSize,
            out int total,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null)
        {
            var result = Query(typeof(T), page, pageSize, out var stats, criteria, from, till, orderby);
            total = Convert.ToInt32(stats.TotalResults);
            return result.OfType<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="references">All references you like to get the entities from</param>
        /// <param name="includeNoAccess">includes no access entities as dummy entity with only the name, date and uid.</param>
        /// <returns></returns>
        public override async Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> references, bool includeNoAccess = false)
        {
            var list = new List<IPersist>();
            using (var session = Db.Store.OpenAsyncSession())
            {
                // 
                var ravenObjs = await session.LoadAsync<object>(references.Select(r => $"{r.Uid}"));

                foreach (var ravenObj in ravenObjs.Select(r => r.Value)) //potential performance improvement when using a parallel foreach here.
                {
                    var metadata = session.Advanced.GetMetadataFor(ravenObj);
                    var p = ravenObj.Presents<IPersist>(
                        (r, o) => MetaDataInitializer.Initialize(r, o, metadata));

                    if (includeNoAccess && !p.CanRead()) // when the object is marked as unreadable from the database source we setup a dummy actor, only having the information refering the the actor.
                    {
                        p = new
                        {
                            p?.Uid,
                            p?.Name,
                            p?.LastModified
                        }.Presents<IPersist>();
                        p.ManagePermission(pr => pr.SetMask(PermissionLevel.None));
                    }

                    list.Add(p);
                }

                return list;
            }
        }

        

        /// <summary>
        /// Appends a single criterion to the query, wherever it sits in the clause tree.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for the operator in front of it: ravendb only falls back to its
        /// own default operator when none was given, and that fallback is an OrElse whenever the
        /// previous where token was a Search, even across a closed subclause.
        /// </remarks>
        private static IDocumentQuery<object> AppendCriteria(IDocumentQuery<object> query, Criteria itm)
        {
            // for safety reasons we only allow a few characters in the fieldname.
            // dynamic fields are always started with a _ during indexing with Roles_BySkillAndReadPermission
            var fieldName = $"_{Regex.Replace(itm.Field, Rgx, string.Empty)}";

            switch (itm.ConditionEnum)
            {
                case ConditionEnum.lt: // 11 - less than
                    return query.WhereLessThan(fieldName, itm.Value);
                case ConditionEnum.gt: // 12 - greater than
                    return query.WhereGreaterThan(fieldName, itm.Value);
                // ---
                case ConditionEnum.ct: // 2 - contains / like
                    // the ngram twin only exists for strings, anything else can not be
                    // searched and degrades to an equality on the exact field.
                    if (itm.Value is not string ctValue)
                        return query.WhereEquals(fieldName, itm.Value);

                    // The "~" twin of the field is indexed with NGramAnalyzer, so every substring
                    // of every word is an indexed term and Search becomes a contains:
                    // 'jo' hits 'john', 'lenjonas' and 'peter jo'.
                    var terms = ToNGramTerms(ctValue);
                    return terms == null
                        // nothing the ngram twin can match (every word is shorter than MinGram),
                        // so degrade to a prefix match on the exact field. It can not be a
                        // Search with a wildcard: ravendb fails the whole query because it can
                        // not build an NGramAnalyzer for a wildcard term, and within this index
                        // every Search resolves to that analyzer.
                        ? query.WhereStartsWith(fieldName, ctValue.Trim())
                        // SearchOperator.And because ToNGramTerms can return several terms for one
                        // search term, and the value has to contain all of them.
                        : query.Search($"{fieldName}{Roles_BySkillAndReadPermission.NGramSuffix}", terms, SearchOperator.And);
                // ReSharper disable once RedundantCaseLabel : we like to show other default values as well here.
                case ConditionEnum.eq: // 1 - equal
                default:
                    return query.WhereEquals(fieldName, itm.Value);
            }
        }

        /// <summary>
        /// Translates the skill, the read permission of the current user, the date range, the criteria
        /// and the ordering into one ravendb query against Roles_BySkillAndReadPermission. Kept apart
        /// from the execution in Query so the generated RQL can be asserted without a server.
        /// </summary>
        internal static IDocumentQuery<object> BuildQuery(IDocumentSession session,
            Type objType,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null,
            DateTimeOffset? till = null,
            string orderby = null)
        {
            var query = session.Advanced
                .DocumentQuery<object>(indexName: Roles_BySkillAndReadPermission._IndexName)
                // general canread and from the right skill
                .WhereEquals("CanRead", true).ContainsAny("Skills", [objType.GetRoleName()]);
            
            if(from.HasValue)
            {
                query = query.AndAlso().WhereGreaterThanOrEqual(nameof(IPersist.LastModified), from.Value);
            }
            
            if(till.HasValue)
            {
                query = query.AndAlso().WhereLessThanOrEqual(nameof(IPersist.LastModified), till.Value);
            }
            
            // never lean on the implicit operator between two clauses: ravendb walks back to the
            // last where token, past any closing bracket, and defaults to OrElse when that token
            // happens to be a Search (which the ct criteria below emit).
            query = query.AndAlso().OpenSubclause() // check for specific user
                    .ContainsAny("UsersCanRead", [UserContext.Current.UserName])
                    .OrElse() // or group rights (wildcard groups supported with *)
                    .ContainsAny("GroupsCanRead", UserContext.Current.Groups.Concat(["*"]).ToArray())
                    .OrElse() // when users or groups are not found
                    .OpenSubclause()
                        .Not.WhereExists("UsersCanRead")
                        .Not.WhereExists("GroupsCanRead")
                    .CloseSubclause()
                .CloseSubclause();
            
            // build the criteria, only indexed dynamic fields are supported;

            var criteriaEnumerable = criteria as Criteria[] ?? criteria?.ToArray();
            if (criteriaEnumerable != null)
            {
                // group all fields in subclauses, one subclause per fieldname.
                var criteriaGroups = criteriaEnumerable.GroupBy(c => c.Field, StringComparer.InvariantCultureIgnoreCase);

                foreach (var grp in criteriaGroups)
                {
                    // a range narrows, so the lt and gt of one fieldname are And-ed together. every
                    // other condition of that same fieldname widens and is Or-ed. both parts are
                    // And-ed with each other, just like the groups of the different fieldnames are.
                    var matches = grp.Where(c => c.ConditionEnum is not (ConditionEnum.lt or ConditionEnum.gt))
                        .OrderBy(c => c.ConditionEnum).ToArray();
                    var ranges = grp.Where(c => c.ConditionEnum is ConditionEnum.lt or ConditionEnum.gt)
                        .OrderBy(c => c.ConditionEnum).ToArray();

                    // And binds stronger than Or in rql, so both parts need brackets of their own
                    // when both are there: 'a or b and c' would read as 'a or (b and c)'.
                    var both = matches.Length > 0 && ranges.Length > 0;

                    query = query.AndAlso().OpenSubclause(); // start a new subclause for the grp

                    if (matches.Length > 0)
                    {
                        if (both) query = query.OpenSubclause();

                        for (var i = 0; i < matches.Length; i++)
                            query = AppendCriteria(i == 0 ? query : query.OrElse(), matches[i]);

                        if (both) query = query.CloseSubclause();
                    }

                    if (ranges.Length > 0)
                    {
                        if (both) query = query.AndAlso().OpenSubclause();

                        for (var i = 0; i < ranges.Length; i++)
                            query = AppendCriteria(i == 0 ? query : query.AndAlso(), ranges[i]);

                        if (both) query = query.CloseSubclause();
                    }

                    query = query.CloseSubclause();
                }
            }
            
            // optionally add the field on which to order the result.
            if (orderby != null)
            {
                var fieldname = Regex.Replace(orderby, Rgx, string.Empty);
                if (!string.IsNullOrEmpty(orderby))
                {
                   query = query.OrderBy(fieldname);
                }
            }

            return query;
        }

        private IEnumerable<IRole> Query(Type objType,
            int page,
            int pageSize,
            out QueryStatistics stats,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null,
            DateTimeOffset? till = null,
            string orderby = null)
        {
            using (var session = Db.Store.OpenSession())
            {
                var criteriaEnumerable = criteria as Criteria[] ?? criteria?.ToArray();

                var query = BuildQuery(session, objType, criteriaEnumerable, from, till, orderby);

                // When debug logging is on, log the underlying RQL generated.
                Logger.LogDebug("RQL generated for {Role} having a total of {CriteriaCount} criteria. Query: '{RQL}', within '{Clss}.{Fn}'",
                    objType.GetRoleName(),
                    criteriaEnumerable?.Length ?? 0,
                    query.ToString(),
                    nameof(RavenPersistedRoleRepository),
                    nameof(Query));

                var result = query
                    .SelectFields<object>() // select the original "underlying" document (server side) .. using OfType<object> is a client side action.
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Statistics(out stats)
                    .ToList() // exucute the query
                    // first execute than clientside projection, make sure to get the original document
                    .Select(ravenObj => // convert to a IRole.
                    {
                        var metadata = session.Advanced.GetMetadataFor(ravenObj);
                        return ravenObj.PresentsType(objType,
                            (r, o) => MetaDataInitializer.Initialize(r as IPersist, o, metadata));
                    }).ToList(); // needs to be executed otherwise storesession can not be accessed within .Select.

                return result;
            }
        }
        
        public void Dispose()
        {
            _uow?.Dispose();
        }
    }
}
