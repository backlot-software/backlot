using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Backlot.Services.LiteDB.Dto;
using LiteDB;
using Newtonsoft.Json;

namespace Backlot.Services.LiteDB;

public class LiteRelationRepository : IRelationRepository
{
    private readonly IUnitOfWork _uow;

    public LiteRelationRepository(IUnitOfWork unitOfWork)
    {
        _uow = unitOfWork;
    }
    
    public LiteRelationRepository()
    {
        _uow = new DummyUnitOfWork();
    }

    private ILiteCollection<RelationEntity> Relations => Db.Store.GetCollection<RelationEntity>("Relations");

    public Task Add(Relation relation)
    {
        var entity = new RelationEntity(relation);
        
        // check existing relation using LiteDB
        
        if (!Relations.Exists(e => e.Id == entity.Id))
        {
            Relations.Insert(entity);
        }
        
        return Task.CompletedTask;
    }

    public void Remove(Relation relation)
    {
        Relations.DeleteMany(e => e.Serialized["Item1.Uid"] == relation.Item1.Uid && e.Serialized["Item2.Uid"] == relation.Item2.Uid);
    }

    public void RemoveAll(RoleReference reference)
    {
        Relations.DeleteMany(e => e.Serialized["Item1.Uid"] == reference.Uid || e.Serialized["Item2.Uid"] == reference.Uid);
    }

    public IEnumerable<RoleReference> GetAll(RoleReference brother)
    {
        var relations = Relations
            .Find(r => r.Serialized["Item1.Uid"]== brother.Uid || r.Serialized["Item2.Uid"] == brother.Uid);
        
        return relations.Select(r => ToRelation(r.Serialized, Strategy.DeSerializeFromTrustedSource).GetRelatedItem(brother));
    }
    
    private static Relation ToRelation(string json, Newtonsoft.Json.JsonSerializer strategy)
    {
        using var reader = new StringReader(json);
        using var jsonReader = new JsonTextReader(reader);
        return strategy.Deserialize<Relation>(jsonReader);
    }
}