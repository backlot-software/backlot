using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Security;

namespace Backlot.Core.Services;

/// <summary>
/// An example relation repository, can be instantiate as a singleton and keeps relations during one run in memory.
/// Used for low traffic none load balanced websites and demo purposes
/// </summary>
public class MemoryPersistedRoleRepository : BasePersistedRoleRepository
{
    public MemoryPersistedRoleRepository()
    {
        MemoryStore.Initialize();
    }

    public override void Terminate(string key)
    {
        MemoryStore.Store.Remove(key);
        MemoryStore.Checksums.Remove(key);
    }

    public override void FlushDb()
    {
        MemoryStore.Store = new Dictionary<string, string>();
        MemoryStore.Relations = new List<Relation>();
        MemoryStore.Checksums = new Dictionary<string, string>();
    }

    public override IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total,
        IEnumerable<Criteria> criteria = null,
        DateTimeOffset? from = null, DateTimeOffset? till = null,
        string orderby = null)
    {
        if (criteria != null || orderby != null)
            throw new NotImplementedException($"criteria and orderby not implemented for {nameof(MemoryPersistedRoleRepository)}");

        var all = MemoryStore.Store
            .Select(stringPair => stringPair.Value.Presents<IPersist>())
            .Where(persist => persist.Skills().Contains(objType.GetRoleName()))
            .ToList();

        total = all.Count;

        return all.Skip(pageSize * (page - 1)).Take(pageSize);
    }

    //private T Present<T>(string json, Type objT)
    //{
    //    return json.PresentsType()
    //}

    public override IEnumerable<T> GetAll<T>(int page, int pageSize, out int total,
        IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null,
        string orderby = null)
    {
        if (criteria != null || orderby != null)
            throw new NotImplementedException($"criteria and orderby not implemented for {nameof(MemoryPersistedRoleRepository)}");

        return GetAll(typeof(T), page, pageSize, out total, criteria, orderby: orderby).Select(role => role.Presents<T>());

        //var all = MemoryStore.Store
        //    .Select(stringPair => stringPair.Value.Presents<IPersist>()) //first present as persist because everything should be IPersist
        //    .Where(role => role.Skills().Contains(nameof(T))) //get only with right skill
        //    .Select(role => role.Presents<T>())
        //    //todo: orderby
        //    //todo: criteria
        //    .ToList();

        //total = all.Count;

        //return all.Skip(pageSize * (page - 1)).Take(pageSize);
    }

    public override Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> refereces, bool includeNoAccess = false)
    {
        var lst = new List<IPersist>();
        foreach (var reference in refereces)
        {
            if (TryGet<IPersist>(reference.Uid, out var role))
            {
                lst.Add(role);
            }
        }

        return Task.FromResult<IEnumerable<IPersist>>(lst);
    }

    public override IEnumerable<Revision> GetRevisions<TR>(string uid)
    {
        //todo: implement revisons in MemoryStore
        if (!MemoryStore.Store.TryGetValue(uid, out var roleString)) return null;
        var role = roleString.Presents<TR>();
        return
        [
            new Revision()
            {
                Reference = role.GetReference(),
                Checksum = role.GetChecksum(),
                Content = role
            }
        ];
    }

    public override bool TryGetPermission(string uid, out Permission permission)
    {
        if (MemoryStore.Store.TryGetValue(uid, out var r))
        {
            // todo: implement permissions (meta data) in MemoryStore
            permission = Permission.Create(PermissionLevel.ReadWriteExecute);
            return true;
        }

        permission = Permission.Create(PermissionLevel.None);
        return false;
    }

    protected override Task<TRole> Store<TRole>(TRole role)
    {
        if(string.IsNullOrEmpty(role.Uid))
            throw new ArgumentNullException($"Uid is null or empty for  {role.GetType().GetRoleName()} with name '{role.Name}', and therefor we can not persist this item.");
    
        StoreOrUpdate(MemoryStore.Checksums, role.Uid, role.GetChecksum());

        role.LastModified = DateTimeOffset.Now;
        var jsonString = role.ToJson(Strategy.SerializeForPersistance);

        StoreOrUpdate(MemoryStore.Store, role.Uid, jsonString);

        return Task.FromResult(role);
    }

    private static void StoreOrUpdate<TK, TV>(IDictionary<TK,TV> dictionary, TK key, TV value)
    {
        dictionary[key] = value;
    }

    protected override bool TryGetType(string uid, Type objType, out IRole obj)
    {
        obj = null;
        if (!MemoryStore.Store.TryGetValue(uid, out var roleString)) return false;
        obj = roleString.PresentsType(objType, (role, o) =>
        {
            if (role is IPersist p)
            {
                p.LastModified = DateTimeOffset.Now;
                return p;
            }
            return role;
            //todo:, Permission.Create(PermissionLevel.ReadWriteExecute), _ => { }); // all roles from this memory persistance example are always readwrite execute 
        }); 
        
        return true;
    }
}
