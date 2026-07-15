namespace Backlot.Studio.ViewModels;

// View models backing the Configuration screen. A flat list of ConfigurationInfo entries is turned
// into a namespace tree (ConfigTreeNode) for the left nav plus, per owning class, a list of editable
// properties (ConfigProperty) each carrying its optional named alternatives (NamedConfig).

/// <summary>
/// A node in the configuration navigation tree. Intermediate nodes are namespace segments
/// (Backlot > Http > Watching …); a node that owns <see cref="Properties"/> is the class the
/// properties belong to. A node can be both (a class that also has child namespaces).
/// </summary>
public class ConfigTreeNode
{
    public string Segment { get; set; } = string.Empty;   // this level's name, e.g. "DebugWatcher"
    public string FullPath { get; set; } = string.Empty;   // cumulative dotted path from the root
    public SortedDictionary<string, ConfigTreeNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ConfigProperty> Properties { get; } = new();

    public bool IsClass => Properties.Count > 0;
}

/// <summary>A single default configuration property ({class}.{property}) and its named alternatives.</summary>
public class ConfigProperty
{
    public string Name { get; set; } = string.Empty;          // full default name: {class}.{property}
    public string ClassPath { get; set; } = string.Empty;     // {class}
    public string PropertyName { get; set; } = string.Empty;  // {property}
    public string? Value { get; set; }
    public bool IsBoolean { get; set; }
    public bool ReadOnly { get; set; }
    public List<NamedConfig> Named { get; set; } = new();
}

/// <summary>A named alternative for a property ({class}.{name}.{property}).</summary>
public class NamedConfig
{
    public string Name { get; set; } = string.Empty;         // full name: {class}.{name}.{property}
    public string ConfigName { get; set; } = string.Empty;   // the {name} segment
    public string? Value { get; set; }
}
