using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using LiteDB;
// ReSharper disable InconsistentNaming

namespace Backlot.Services.LiteDB.Dto;

/// <summary>
/// We need a relation entity, because LiteDB does not support the Core.Relation object
/// </summary>
internal class RelationEntity
{
    private string RelationIdSeparator
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(field)) return field;

            var config = ServiceLocator.Get<IConfigurationManager>()
                .Get<Settings>(s => s.RelationIdSeperator);
            
            field = string.IsNullOrWhiteSpace(config) ? "_-_" : config;

            return field;
        }
    }
    
    [BsonId]
    public string Id { get; private set; }

    public RelationEntity()
    {
        // id is set by LiteDb.
        // Serialized is set by LiteDb.
    }
    
    public RelationEntity(Relation relation)
    {
        Id = $"{relation.Item1.Uid}{RelationIdSeparator}{relation.Item2.Uid}";
        Serialized = JsonSerializer.Deserialize(relation.ToJson(Strategy.SerializeForPersistance)).AsDocument;
    }
    
    public BsonDocument Serialized { get; set; }
}