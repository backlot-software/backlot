namespace Backlot.Studio.Models.Api;

/// <summary>
/// One entry from GET /api/role/director/configurationinfos. A configuration entry is addressed by
/// a fully-qualified <see cref="Name"/> of the form {class}.{property} (a default value) or
/// {class}.{name}.{property} (a "named" alternative). <see cref="ConfigurationType"/> is the
/// assembly-qualified CLR type — only System.String and System.Boolean are edited by Studio.
/// </summary>
public class ConfigurationInfo
{
    public string Name { get; set; } = string.Empty;
    public string ConfigurationType { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool ReadOnly { get; set; }

    /// <summary>True when the underlying property is a System.Boolean (rendered as a toggle).</summary>
    public bool IsBoolean =>
        ConfigurationType.StartsWith("System.Boolean", StringComparison.Ordinal);
}
