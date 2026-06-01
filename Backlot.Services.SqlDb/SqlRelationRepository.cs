using Backlot.Core;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Services.SqlDb.Dto;
using Newtonsoft.Json;
using SqlKata.Execution;

namespace Backlot.Services.SqlDb;

/// <summary>
/// Sql implementation of the relation repository.
/// Can be used by Dynamic as well as Relational role repositories.
/// </summary>
public class SqlRelationRepository : IRelationRepository, IDisposable
{
    public const string DynamicRelationTableName = "DynamicRelationStore";

    private readonly QueryFactory _db;
    
    public SqlRelationRepository()
    {
        _db = Db.Store();
    }
    
    public async Task Add(Relation relation)
    {
        // check existing relation using SqlKata
        var chk = _db.Query(DynamicRelationTableName)
            .Where(nameof(RelationEntity.Role1_Uid), relation.Item1.Uid)
            .Where(nameof(RelationEntity.Role2_Uid), relation.Item2.Uid);

        if (await _db.FirstOrDefaultAsync(chk) == null)
        {
            var q = _db.Query(DynamicRelationTableName)
                .AsInsert(new RelationEntity
                {
                    Role1_Uid = relation.Item1.Uid,
                    Role2_Uid = relation.Item2.Uid,
                    Serialized = relation.ToJson(Db.Serializer)
                });

            await _db.ExecuteAsync(q);
        }
    }

    public void Remove(Relation relation)
    {
        var query = _db.Query(DynamicRelationTableName)
            .Where(nameof(RelationEntity.Role1_Uid), relation.Item1.Uid)
            .Where(nameof(RelationEntity.Role2_Uid), relation.Item2.Uid);

        _db.Execute(query);
    }

    public void RemoveAll(RoleReference role)
    {
        var query = _db.Query(DynamicRelationTableName)
            .Where(nameof(RelationEntity.Role1_Uid), role.Uid)
            .Or()
            .Where(nameof(RelationEntity.Role2_Uid), role.Uid);

        _db.Execute(query);
    }

    public IEnumerable<RoleReference> GetAll(RoleReference brother)
    {
        var query = _db.Query(DynamicRelationTableName)
            .Where(nameof(RelationEntity.Role1_Uid), brother.Uid)
            .Or()
            .Where(nameof(RelationEntity.Role2_Uid), brother.Uid);

        var relations = _db.Get<RelationEntity>(query);

        foreach (var rel in relations)
        {
            var relation = ToRelation(rel.Serialized, Db.Deserializer);
            if (relation != null)
                yield return relation.Item1.Uid == brother.Uid ? relation.Item2 : relation.Item1;
        }
    }
    
    private static Relation ToRelation(string json, JsonSerializer strategy)
    {
        using var reader = new StringReader(json);
        using var jsonReader = new JsonTextReader(reader);
        return strategy.Deserialize<Relation>(jsonReader);
    }
    
    

    public void Dispose()
    {
        _db.Dispose();
        //_uow?.Dispose();
    }
}