using System;
using LiteDB;

namespace Backlot.Services.LiteDB.Dto;

/// <summary>
/// Role Actor wrappers with fields for indexing and fast querying.
/// </summary>
internal class StoreEntity
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    
    public string[] Skills { get; set; } = [];
    public DateTimeOffset? LastModified { get; set; }
    public bool CanRead { get; set; }
    public string[] UsersCanRead { get; set; } = [];
    public string[] GroupsCanRead { get; set; } = [];
    
    public BsonDocument Data { get; set; } = new BsonDocument();
    
    public string Permission { get; set; }
}
