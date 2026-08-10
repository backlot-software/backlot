namespace Backlot.Studio.Models.Api;

/// <summary>
/// One role-type entry from GET /api/role/director/roles.
/// Type strings are .NET FriendlyName values (String, Int32, Boolean, Decimal, ...).
/// </summary>
public class RoleSchema
{
    public string Role { get; set; } = "";
    public List<FieldSchema> Fields { get; set; } = [];
    public List<string> Skills { get; set; } = [];
}

/// <summary>One editable field in a role's schema.</summary>
public class FieldSchema
{
    public string Field { get; set; } = "";
    public string Type { get; set; } = "";
    public List<CharacteristicSchema> Characteristics { get; set; } = [];
}

/// <summary>
/// A field characteristic. The only read-only signal in the framework is
/// Characteristic == "Calculated"; validation attributes (Required, StringLength,
/// Range, ...) also surface here but are NOT read-only. Parameters omitted for v1.
/// </summary>
public class CharacteristicSchema
{
    public string Characteristic { get; set; } = "";
}
