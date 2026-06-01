using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.LiteDB.Dto;
using LDB = LiteDB;

namespace Backlot.Services.LiteDB;

public class LitePersistedRoleRepository : BasePersistedRoleRepository
{
    private readonly IUnitOfWork _uow;
    private const string Rgx = @"[^A-Za-z0-9_\-.@]";

    public LitePersistedRoleRepository(IUnitOfWork unitOfWork)
    {
        _uow = unitOfWork;
    }
    
    public LitePersistedRoleRepository()
    {
        _uow = new DummyUnitOfWork();
    }

    private LDB.ILiteCollection<StoreEntity> Roles => Db.Store.GetCollection<StoreEntity>("Roles");

    protected override Task<TRole> Store<TRole>(TRole obj)
    {
        var entity = new StoreEntity
        {
            Id = obj.Uid,
            CanRead = obj.CanRead(),
            UsersCanRead = obj.Permission().UserLevels.Where(l => l.Value >= PermissionLevel.Read).Select(x => $"{x.Key}").ToArray(),
            GroupsCanRead =  obj.Permission().GroupLevels.Where(l => l.Value >= PermissionLevel.Read).Select(x => $"{x.Key}").ToArray(),
            LastModified = DateTimeOffset.Now,
            Skills = obj.Skills().ToArray(),
            Permission = obj.Permission().ToString(),
            Data = LDB.JsonSerializer.Deserialize(obj.ToJson(Strategy.SerializeForPersistance)).AsDocument
        };
        Roles.Upsert(entity);
        return Task.FromResult(obj);
    }

    protected override bool TryGetType(string uid, Type objType, out IRole obj)
    {
        var entity = Roles.FindById(uid);
        if (entity != null)
        {
            var rawJson = LDB.JsonSerializer.Serialize(entity.Data);
            obj = rawJson.PresentsType(objType, (r, _) => StoreEntityMetaDataInitializer.Initialize(r as IPersist, entity));
            return true;
        }

        obj = Acting.New(objType);
        return false;
    }

    public override Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> references, bool includeNoAccess = false)
    {
        var uids = references.Select(r => r.Uid).ToArray();
        var entities = Roles.Find(r => ((IEnumerable<string>)uids).Contains(r.Id));
        
        return Task.FromResult<IEnumerable<IPersist>>(entities.Select(entity =>
        {
            var rawJson = LDB.JsonSerializer.Serialize(entity.Data);
            var p = rawJson.Presents<IPersist>((r, _) => StoreEntityMetaDataInitializer.Initialize(r, entity));
            
            if (includeNoAccess && !p.CanRead())
            {
                var dummy = new // create a dummy with no access references.
                {
                    p.Uid,
                    p.Name,
                    p.LastModified
                }.Presents<IPersist>();
                dummy.ManagePermission(pr => pr.SetMask(PermissionLevel.None));
                return dummy;
            }
            
            return p;
        }).ToList());
    }

    public override IEnumerable<Revision> GetRevisions<TR>(string uid)
    {
        if (TryGet<TR>(uid, out var obj))
        {
            return
            [
                new Revision
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
        var entity = Roles.FindById(uid);
        if (entity != null)
        {
            permission = Permission.Deserialize(entity.Permission);
            return true;
        }

        permission = Permission.Create(PermissionLevel.None);
        return false;
    }

    public override void Terminate(string key)
    {
        Roles.Delete(key);
    }

    public override void FlushDb()
    {
        Roles.DeleteMany(_ => true);
        
    }

    public override IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total,
        IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null,
        string orderby = null)
    {
        LDB.BsonExpression queryExp = LDB.BsonExpression.Create("1=1"); // Without 1=1, the code would need extra logic to handle the first condition:

        // Base filters: CanRead and Skill
        queryExp = LDB.Query.And(queryExp, LDB.Query.EQ(nameof(StoreEntity.CanRead), true));
        queryExp = LDB.Query.And(queryExp, LDB.Query.Any().EQ($"{nameof(StoreEntity.Skills)}",objType.GetRoleName())); // todo: this is correct: AND "Formula" IN $.Skills we need to find the expression equivalent.

        // Date range filters
        if (from.HasValue)
        {
            queryExp = LDB.Query.And(queryExp, LDB.Query.GTE(nameof(StoreEntity.LastModified), from.Value.UtcDateTime));
        }

        if (till.HasValue)
        {
            queryExp = LDB.Query.And(queryExp, LDB.Query.LTE(nameof(StoreEntity.LastModified), till.Value.UtcDateTime));
        }

        // Security filters
        var user = UserContext.Current.UserName;
        var groups = UserContext.Current.Groups.Concat(["*"]).Select(x => new LDB.BsonValue(x)).ToArray();
        
        var securityExpressions = new List<LDB.BsonExpression>();
        securityExpressions.Add(LDB.Query.Any().EQ(nameof(StoreEntity.UsersCanRead), user));
        securityExpressions.AddRange(groups.Select(g => LDB.Query.Any().EQ($"{nameof(StoreEntity.GroupsCanRead)}", g)));
        securityExpressions.Add(LDB.Query.And(
            LDB.BsonExpression.Create($"COUNT({nameof(StoreEntity.UsersCanRead)}) = 0"),
            LDB.BsonExpression.Create($"COUNT({nameof(StoreEntity.GroupsCanRead)}) = 0")
        ));
        var securityQuery = LDB.Query.Or(securityExpressions.ToArray());
        
        queryExp = LDB.Query.And(queryExp, securityQuery);

        // Criteria filters
        var criteriaEnumerable = criteria as Criteria[] ?? criteria?.ToArray();
        if (criteriaEnumerable != null)
        {
            var criteriaGroups = criteriaEnumerable.GroupBy(c => c.Field, StringComparer.InvariantCultureIgnoreCase);

            foreach (var grp in criteriaGroups)
            {
                var orExpressions = new List<LDB.BsonExpression>();
                var andExpressions = new List<LDB.BsonExpression>();
                
                foreach (var itm in grp.OrderBy(c => c.ConditionEnum))
                {
                    var fieldName = $"Data.{Regex.Replace(itm.Field, Rgx, string.Empty)}";
                    var bsonValue = itm.Value switch
                    {
                        DateTimeOffset dto => new LDB.BsonValue(dto.UtcDateTime),
                        DateTime dt => new LDB.BsonValue(dt),
                        _ => new LDB.BsonValue(itm.Value)
                    };

                    LDB.BsonExpression expr = itm.ConditionEnum switch
                    {
                        ConditionEnum.lt => LDB.Query.LT(fieldName, bsonValue),
                        ConditionEnum.gt => LDB.Query.GT(fieldName, bsonValue),
                        ConditionEnum.ct => LDB.Query.Contains(fieldName, itm.Value?.ToString() ?? string.Empty),
                        _ => LDB.Query.EQ(fieldName, bsonValue)
                    };

                    if (itm.ConditionEnum is ConditionEnum.lt or ConditionEnum.gt)
                    {
                        andExpressions.Add(expr);
                    }
                    else
                    {
                        orExpressions.Add(expr);
                    }
                }

                if (andExpressions.Any())
                {
                    orExpressions.Add(andExpressions.Count > 1 ? LDB.Query.And(andExpressions.ToArray()) : andExpressions[0]);
                }

                if (orExpressions.Any())
                {
                    var combinedGroupQuery = orExpressions.Count > 1 ? LDB.Query.Or(orExpressions.ToArray()) : orExpressions[0];
                    queryExp = LDB.Query.And(queryExp, combinedGroupQuery);
                }
            }
        }

        total = Roles.Count(queryExp);
        
        // Execute the query
        var queryable = Roles.Query().Where(queryExp);
        
        if (!string.IsNullOrEmpty(orderby))
        {
            var fieldName = Regex.Replace(orderby, Rgx, string.Empty);
            var topLevelFields = new[] { "Id", "LastModified", "CanRead", "Permission" };
            var sortField = Enumerable.Contains(topLevelFields, fieldName, StringComparer.OrdinalIgnoreCase) ? fieldName : $"Data.{fieldName}";
            queryable = queryable.OrderBy(sortField);
        }

        var entities = queryable
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToEnumerable();

        return entities.Select(entity =>
        {
            var rawJson = LDB.JsonSerializer.Serialize(entity.Data);
            return rawJson.PresentsType(objType, (r, _) => StoreEntityMetaDataInitializer.Initialize(r as IPersist, entity));
        }).ToList();
    }

    public override IEnumerable<T> GetAll<T>(int page, int pageSize, out int total, IEnumerable<Criteria> criteria = null,
        DateTimeOffset? from = null, DateTimeOffset? till = null, string orderby = null)
    {
        var result = GetAll(typeof(T), page, pageSize, out total, criteria, from, till, orderby);
        return result.OfType<T>();
    }
}