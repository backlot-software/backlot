using Newtonsoft.Json;

namespace Backlot.Defaults.Scenarios.Configuration.Models;

/// <summary>
/// INTERNAL: A result object for internal use only.
/// Can be changed without notice.
/// </summary>
public class FieldResultItem
{
    public string Field { get; set; }
    public string Type { get; set; }
    
    [JsonIgnore]
    public Type FieldType { get; set; }
    
    public IEnumerable<CharacteristicResultItem> Characteristics { get; set; }
}