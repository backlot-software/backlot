namespace Backlot.Defaults.Scenarios.Configuration.Models;

/// <summary>
/// INTERNAL: A result object for internal use only.
/// Can be changed without notice.
/// </summary>
public class CharacteristicResultItem
{
    public string? Characteristic { get; set; }
    public IEnumerable<ParameterResultItem> Parameters { get; set; }
}