using Backlot.Core;
using Backlot.Core.Services;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Backlot.Services.RavenDb;

/// <summary>
/// Store relations into the ravendb (forever).
/// For development purpose you can also use the MemoryRelationReposity.
/// </summary>
public class RavenRelationRepository : IRelationRepository, IDisposable
{
    private readonly HashSet<Relation> _handledRelations = [];

    public async Task Add(Relation relation)
    {
        if (relation.Item1 == relation.Item2)
            throw new ArgumentException("You are trying to create a circular reference.");

        if (!_handledRelations.Contains(relation)) //if not already handled by this instance.
        {
            using (var session = Db.Store.OpenAsyncSession()) // todo: relations currently do not use the unit of work,
                                                              // todo: this can cause in a situation a relation is saved but the object is not available. consider to use the unit of work,
                                                              // todo: but keep in mind the relation needs to be available also when not stored yet.
            {
                var list = await session.Query<Relation>()
                    .Where(r => (r.Item1.Uid == relation.Item1.Uid || r.Item1.Uid == relation.Item2.Uid)
                                && (r.Item2.Uid == relation.Item2.Uid || r.Item2.Uid == relation.Item1.Uid))
                    .ToListAsync();

                if (!list.Any()) //only add when not already created at db level
                {
                    await session.StoreAsync(relation);
                    await session.SaveChangesAsync();
                }
            }
        }
    }

    public void Remove(Relation relation)
    {
        using (var session = Db.Store.OpenSession())
        {
            var record = session.Query<Relation>().FirstOrDefault(r => r.Equals(relation));
            if (record != default(Relation))
            {
                session.Delete(record);
            }
        }
    }

    public void RemoveAll(RoleReference role)
    {
        using (var session = Db.Store.OpenSession())
        {
            var records = session.Query<Relation>()
                .Where(itm => itm.Item1.Uid == role.Uid || itm.Item2.Uid == role.Uid);

            foreach (var relation in records)
            {
                Remove(relation);
            }
        }
    }

    public IEnumerable<RoleReference> GetAll(RoleReference brother)
    {
        using (var session = Db.Store.OpenSession())
        {
            var relations = session.Query<Relation>()
                .Where(r => (r.Item1.Uid == brother.Uid || r.Item2.Uid == brother.Uid)).ToHashSet();
            
            _handledRelations.UnionWith(relations);
            
            return relations.Select(r => r.GetRelatedItem(brother));
        }
    }

    public void Dispose()
    {
        //_uow?.Dispose();
    }
}