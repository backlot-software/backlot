using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Roles;

namespace Backlot.Core.Services;

/// <summary>
/// An example relation repository, can be instantiate as a singleton and keeps relations during one run in memory.
/// Used for low traffic none load balanced websites and demo purposes.
/// </summary>
public class MemoryRelationRepository : IRelationRepository
{
    public MemoryRelationRepository()
    {
        MemoryStore.Initialize();
    }

    public Task Add(Relation relation)
    {
        if (relation.Item1 == relation.Item2)
            throw new ArgumentException("You are trying to create a circular reference.");
        
        var list = MemoryStore.Relations
            .Where(r => (r.Item1.Uid == relation.Item1.Uid || r.Item1.Uid == relation.Item2.Uid)
                        && (r.Item2.Uid == relation.Item2.Uid || r.Item2.Uid == relation.Item1.Uid));
        if (!list.Any())
        {
            MemoryStore.Relations.Add(relation);
        }

        return Task.CompletedTask;
    }

    public void Remove(Relation relation)
    {
        var record = MemoryStore.Relations.FirstOrDefault(r => r.Equals(relation));
        if (record != default(Relation))
        {
            MemoryStore.Relations.Remove(record);
        }
    }

    public void RemoveAll(RoleReference role)
    {
        var records = MemoryStore.Relations
            .Where(itm => itm.IsRelated(role));
        
        foreach(var relation in records)
        {
            Remove(relation);
        }
    }

    public IEnumerable<RoleReference> GetAll(RoleReference brother)
    {
        return MemoryStore.Relations.Where(r => r.IsRelated(brother)).Select(r => r.Related(brother));
    }

    public void RemoveAll(IPersist role)
    {
        RemoveAll(role.GetReference());
    }

    public IEnumerable<RoleReference> GetAll(IPersist brother)
    {
        return MemoryStore.Relations
            .Where(r => (r.Item1.Uid == brother.Uid || r.Item2.Uid == brother.Uid))
            .Select(r => r.GetRelatedItem(brother)).ToList();
    }
}
