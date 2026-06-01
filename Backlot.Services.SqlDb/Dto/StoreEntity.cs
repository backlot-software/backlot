namespace Backlot.Services.SqlDb.Dto;

/// <summary>
/// INTERNAL: DataTransferObject used by SqlKata
/// </summary>
internal class StoreEntity
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public string Checksum { get; set; }
    
    public bool CanRead { get; set; }
    public string UsersCanRead { get; set; }
    public string GroupsCanRead { get; set; }
    public string Permission { get; set; }
    public string Skills { get; set; }
    
    public DateTimeOffset LastModified { get; set; }
    public string JsonData { get; set; }
}