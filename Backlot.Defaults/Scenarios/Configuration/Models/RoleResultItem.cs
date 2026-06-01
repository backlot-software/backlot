#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Backlot.Defaults.Scenarios.Configuration.Models;


/// <summary>
/// INTERNAL: A result object for internal use only.
/// Can be changed without notice.
/// </summary>
public class RoleResultItem
{
    public string Role { get; set; }
    public IEnumerable<FieldResultItem> Fields { get; set; }
    public IEnumerable<string> Skills { get; set; }
}