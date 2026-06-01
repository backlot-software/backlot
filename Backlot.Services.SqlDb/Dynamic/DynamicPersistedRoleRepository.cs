using System.Data;
using System.Text.RegularExpressions;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.SqlDb.Dto;
using Dapper;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Execution;

namespace Backlot.Services.SqlDb.Dynamic
{
    /// <summary>
    /// Stores entities in a Sql database.
    /// TODO: does not support the IUnitOfWork yet.
    /// </summary>
    public sealed class DynamicPersistedRoleRepository : BasePersistedRoleRepository, IDisposable
    {
        private const string Rgx =  @"[^A-Za-z0-9_\-.@]";
        
        // todo: these items are candidate for future configurables.
        public const string DynamicStoreTableName = "DynamicRoleStore"; 
        public const string DefaultMaxStringLength = "4000"; // is a string because we allow "MAX" as well.
        
        private readonly QueryFactory _db;

        public DynamicPersistedRoleRepository()
        {
            _db = Db.Store(); // Initialize once in the constructor
        }

        
        protected override async Task<TRole> Store<TRole>(TRole obj)
        {
            var now = DateTime.UtcNow;

            var s = new StoreEntity()
            {
                Uid = obj.Uid,
                Name = obj.Name,
                Checksum = obj.GetChecksum(),
                
                // permission indexation
                Permission = obj.Permission().ToString(),
                UsersCanRead = string.Join(",", obj.Permission().UserLevels.Where(l => l.Value >= PermissionLevel.Read).Select(x => $"{x.Key}")),
                GroupsCanRead = string.Join(",", obj.Permission().GroupLevels.Where(l => l.Value >= PermissionLevel.Read).Select(x => $"{x.Key}")),
                CanRead = obj.Permission().MaskLevel >= PermissionLevel.Read,
                
                Skills = string.Join(",", obj.Skills()),
                
                LastModified = now,
                JsonData = Json.ToJson(obj, Db.Serializer)
            };
            
            var q = _db.Query(DynamicStoreTableName)
                .Where(nameof(StoreEntity.Uid), s.Uid);

            var stored = await _db.FirstOrDefaultAsync<StoreEntity>(q);

            if (stored == null)
            {
                q = q.AsInsert(s);
                obj.LastModified = now;
                await _db.ExecuteAsync(q); // todo: create support for an IUnitOfWork using Transations.
            }
            else if (stored.Checksum != s.Checksum) // only update if the checksum differs.
            {
                q = q.AsUpdate(s);
                obj.LastModified = now;
                await _db.ExecuteAsync(q); // todo: create support for an IUnitOfWork using Transations.
            }

            return obj;
        }

        protected override bool TryGetType(string uid, Type objType, out IRole obj)
        {
            if (TryGetStoredEntityWhereFieldIs(nameof(StoreEntity.Uid), uid, out var entity))
            {
                obj = entity.JsonData.PresentsType(objType, (r, _) => StoreEntityMetaDataInitializer.Initialize(r as IPersist, entity));
                return true;
            }

            obj = null;
            return false;
        }

        public override async Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> refereces, bool includeNoAccess = false)
        {
            var refIds = refereces.Select(r => r.Uid).ToArray();
            // Build a DataTable matching BulkIdList definition
            var tvp = new DataTable();
            tvp.Columns.Add(nameof(StoreEntity.Uid), typeof(string));  // must match the column name in the UDTT
            foreach (var id in refIds)
            {
                tvp.Rows.Add(id);
            }

            // Add as TVP parameter
            var dp = new DynamicParameters();
            dp.Add("@Ids", tvp.AsTableValuedParameter("dbo.BulkIdList"));

            // Inline query with JOIN against TVP
            var sql = @$"
                SELECT s.*
                FROM dbo.{DynamicStoreTableName} AS s
                JOIN @Ids AS i ON i.Uid = s.Uid;";

            Logger.LogDebug("SQL generated for GetBulk having a total of {RefCount} references. Query: '{SQL}', within '{Clss}.{Fn}'",
                refIds.Length,
                sql,
                nameof(DynamicPersistedRoleRepository),
                nameof(GetBulk));

            // Execute through SqlKata (delegates to Dapper under the hood)
            var results = await _db.SelectAsync<StoreEntity>(sql, dp);
            return results.Select(entity =>
            {
                return entity.JsonData.Presents<IPersist>(
                    (r, _) => StoreEntityMetaDataInitializer.Initialize(r, entity));
            });
        }

        public override IEnumerable<Revision> GetRevisions<TR>(string uid)
        {
            // no support for revisions in this dynamic role store, because of performance reasons.
            if (TryGet<TR>(uid, out var obj))
            {
                // return the one and only obj.
                return
                [
                    new Revision()
                {
                    Checksum = obj.GetChecksum(),
                    Content = obj,
                    Reference = obj.GetReference()
                }
                ];
            }
            
            return [];
        }

        public override bool TryGetPermission(string uid, out Permission permission)
        {
            if (TryGetStoredEntityWhereFieldIs(nameof(StoreEntity.Uid), uid, out var entity))
            {
                permission = Permission.Deserialize(entity.Permission);
                return true;
            }

            permission = Permission.Create(PermissionLevel.None);
            return false;
        }
        
        public override IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null)
        {
            
            var validFields = objType.GetFieldInfo(false).ToArray(); // fields valid to be criteria
            // select criteria to be one of the valid fields
            var validCriteria = criteria == null ? [] : criteria.Where(c => validFields.Any(vf => vf.Name == c.Field)).ToList();
            
            #region build BaseQuery for ;WITH base AS () - for performance
            
            var baseQuery = new Query($"dbo.{DynamicStoreTableName} as r")
                    .Where("r.CanRead", true);
            
            if (from.HasValue)
                baseQuery = baseQuery.Where($"r.{nameof(StoreEntity.LastModified)}", ">=", from.Value.UtcDateTime);
            if (till.HasValue)
                baseQuery = baseQuery.Where($"r.{nameof(StoreEntity.LastModified)}", "<=", till.Value.UtcDateTime);

            //shrink base with uid and name criteria
            
            if (validCriteria.Any(c => c.Field == nameof(IPersist.Uid)))
            {
                var criteriaUid = validCriteria.First(c => c.Field == nameof(IPersist.Uid));
                
                baseQuery = criteriaUid.ConditionEnum == ConditionEnum.ct ?
                    baseQuery.WhereContains("r.Uid", criteriaUid.Value) :
                    baseQuery.Where("r.Uid", criteriaUid.Value);
            }
            
            if(validCriteria.Any(c => c.Field == nameof(IPersist.Name)))
            {
                var criteriaName = validCriteria.First(c => c.Field == nameof(IPersist.Name));
                baseQuery = criteriaName.ConditionEnum == ConditionEnum.ct ?
                    baseQuery.WhereContains("r.Name", criteriaName.Value) :
                    baseQuery.Where("r.Name", criteriaName.Value);
            }
            
            // shrink base with skills, and permission criteria
            
            baseQuery = baseQuery
                    .WhereExists(q => q 
                        .FromRaw("STRING_SPLIT(r.Skills, ',') as s")
                        .SelectRaw("1")
                        .Where("s.value", objType.GetRoleName())
                    )
                    .Where(group => group
                        .Where(inner => inner
                            .WhereRaw("COALESCE(r.GroupsCanRead, '') = ''")
                            .WhereRaw("COALESCE(r.UsersCanRead, '') = ''")
                        )
                        .OrWhereExists(q => q
                            .FromRaw("STRING_SPLIT(r.UsersCanRead, ',') as u")
                            .SelectRaw("1")
                            .Where("u.value", UserContext.Current.UserName)
                        )
                        .OrWhereExists(q => q
                            .FromRaw("STRING_SPLIT(r.GroupsCanRead, ',') as g")
                            .SelectRaw("1")
                            .WhereIn("g.value", UserContext.Current.Groups.Concat(["*"]).ToArray())
                        )
                    );
            
            #endregion

            
            #region Build main Query using the shinked base results.

            var jsnWithFields = new List<(string, string)>();
            foreach (var parameter in validCriteria
                         .SkipWhile(c =>
                             !validFields.Any(f => f.Name == c.Field) ||
                             c.Field == nameof(IPersist.Uid) ||
                             c.Field == nameof(IPersist.Name)))
            {
                var fieldType = "NVARCHAR(MAX)";
                switch (parameter.Value.GetType())
                {
                    case { } t when t == typeof(string):
                        fieldType =
                            $"NVARCHAR({DefaultMaxStringLength})"; // does use a setting for default nvarchar sizes - theorically the ideal length can differ per implementation. "MAX" is highest.
                        break;
                    case { } t when t == typeof(int) || t == typeof(long) || t == typeof(double) ||
                                    t == typeof(float):
                        fieldType = "FLOAT";
                        break;
                    case { } t when t == typeof(bool):
                        fieldType = "BIT";
                        break;
                    case { } t when t == typeof(DateTime) || t == typeof(DateTimeOffset):
                        fieldType = "DATETIMEOFFSET";
                        break;
                    // Add more cases as needed for other types
                }

                // we can use paramter.Field because previously it is already checked against validFields.
                jsnWithFields.Add((parameter.Field, fieldType));
            }

            var query = _db.Query();

            if (!jsnWithFields.Any()) // no json fields then do not execute OPENJSON.
            {
                query = query
                    .With("base", baseQuery)
                    .FromRaw(@$"
                base AS roles
            ");
            }
            else // if any json fields are used for criteria - use OPENJSON
            {
                // do not use more and include more fields then needed;
                var opnJsnWith = string.Join(", ", jsnWithFields.Select(f => $"{f.Item1} {f.Item2} '$.{f.Item1}'"));
                var selectFields = string.Join(", ", jsnWithFields.Select(f => $"jsn.{f.Item1}"));

                query = query
                    .With("base", baseQuery)
                    .FromRaw(@$"
                base AS roles
                CROSS APPLY OPENJSON(roles.JsonData) WITH ({opnJsnWith}) AS jsn
            ")
                    .SelectRaw($"roles.*, {selectFields}");

                // then build jsn criteria on base results.

                // cirteria generation here
                // Criteria generation
                if (criteria != null)
                {
                    var criteriaGroups =
                        validCriteria.GroupBy(c => c.Field, StringComparer.InvariantCultureIgnoreCase);

                    foreach (var group in criteriaGroups)
                    {
                        query = query.Where(groupQuery =>
                        {
                            var needsOr = false;
                            var ltgtConditions = new List<(string FieldName, string Operator, object Value)>();

                            foreach (var item in
                                     group.OrderBy(c =>
                                         c.ConditionEnum)) // loop through the criteria eq and ct first, than lt and gt
                            {
                                // make sure field names are sanitized
                                var fieldName = $"jsn.{Regex.Replace(item.Field, Rgx, string.Empty)}";

                                switch (item.ConditionEnum)
                                {
                                    case ConditionEnum.eq:
                                        if (needsOr) groupQuery = groupQuery.OrWhere(fieldName, item.Value);
                                        else groupQuery = groupQuery.Where(fieldName, item.Value);
                                        needsOr = true;
                                        break;

                                    case ConditionEnum.ct:
                                        if (needsOr)
                                            groupQuery = groupQuery.OrWhereLike(fieldName, $"%{item.Value}%");
                                        else groupQuery = groupQuery.WhereLike(fieldName, $"%{item.Value}%");
                                        needsOr = true;
                                        break;

                                    case ConditionEnum.lt:
                                        ltgtConditions.Add((fieldName, "<", item.Value));
                                        break;

                                    case ConditionEnum.gt:
                                        ltgtConditions.Add((fieldName, "<", item.Value));
                                        break;
                                }
                            }

                            if (ltgtConditions.Any()) // custom subgroup with ands for lt and gt
                            {
                                var ltgtQuery = (Query q) =>
                                {
                                    foreach (var condition in ltgtConditions)
                                    {
                                        q = q.Where(condition.FieldName, condition.Operator, condition.Value);
                                    }

                                    return q;
                                };

                                if (needsOr) groupQuery = groupQuery.OrWhere(ltgtQuery);
                                else groupQuery = groupQuery.Where(ltgtQuery);
                            }

                            return groupQuery;
                        });
                    }
                }
            }

            #endregion

            // order by;

            if (!string.IsNullOrEmpty(orderby))
            {
                var orderbyName = orderby == nameof(IPersist.Uid) || orderby == nameof(IPersist.Name) ||
                                  orderby == nameof(StoreEntity.LastModified)
                    ? orderby
                    : $"jsn.{Regex.Replace(orderby, Rgx, string.Empty)}";
                query = query.OrderBy(orderbyName);
            }
            else
                query = query.OrderBy(nameof(StoreEntity.LastModified));

            // compile;

            var sql = _db.Compiler.Compile(query).Sql;

            Logger.LogDebug(
                "SQL generated for {Role} having a total of {CriteriaCount} criteria. Query: '{SQL}', within '{Clss}.{Fn}'",
                objType.GetRoleName(),
                validCriteria.Count(),
                sql,
                nameof(DynamicPersistedRoleRepository),
                nameof(GetAll));

            var result = _db.Paginate<StoreEntity>(query, page, pageSize);
            total = Convert.ToInt32(result
                .Count); // total results over all pages. throws an overflow exception when the count is larger than int.MaxValue.

            return result.List.Select(entity =>
            {
                return entity.JsonData.PresentsType(objType,
                    (r, _) => StoreEntityMetaDataInitializer.Initialize(r as IPersist, entity));
            });
        }

        public override IEnumerable<T> GetAll<T>(int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null, string orderby = null)
        {
            var result = GetAll(typeof(T), page, pageSize, out total, criteria, from, till, orderby);
            return result.OfType<T>();
        }

        public override void Terminate(string key)
        {
            var q = _db.Query(DynamicStoreTableName).Where("Uid", key);
            _db.Execute(q); // todo: Unit of work and transactions.
        }

        public override void FlushDb()
        {
            throw new NotSupportedException("Flushing a database is not supported for SQL databases");
        }
        
        private bool TryGetStoredEntityWhereFieldIs(string field, string value, out StoreEntity entity)
        {
            var q = _db.Query(DynamicStoreTableName)
                .Where(field, value);

            var results = _db.Get<StoreEntity>(q);
            var storeEntities = results as StoreEntity[] ?? results.ToArray();

            if (storeEntities.Any())
            {
                entity = storeEntities[0];
                return true;
            }

            entity = null;
            return false;
        }
        
        
        public void Dispose()
        {
            // Dispose resources if necessary
            _db.Dispose();
            //todo: we need to implement the IUnitOfWork interface to handle transactions properly.
        }
    }
}