// ReSharper disable InconsistentNaming : Nameing used in Sql

namespace Backlot.Services.SqlDb.Dto;

/// <summary>
/// INTERNAL: DataTransferObject used by SqlKata
/// </summary>
internal class RelationEntity
{
    public string Role1_Uid { get; set; }
    public string Role2_Uid { get; set; }
    
    public string Serialized { get; set; }
}