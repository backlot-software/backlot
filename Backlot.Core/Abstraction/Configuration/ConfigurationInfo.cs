using System;

namespace Backlot.Core.Abstraction.Configuration;

public interface IConfigurationInfo : IRole
{
    /// <summary>
    /// Format is {namespace}.{classname}.{propertyname}
    /// </summary>
    string Name { get; set; }
    object Value { get; set; }
}

public class ConfigurationInfo : IConfigurationInfo
{
    /// <summary>
    /// Format is {namespace}.{classname}.{propertyname}
    /// </summary>
    public string Name { get; set; }
    
    public Type ConfigurationType { get; set; } //todo: ConfigurationType.ConstructName() for serialization / deserialization

    public object Value { get; set; }
    
    /// <summary>
    /// Readonly is set to true for settings not yet possible to manage from a UI. 
    /// </summary>
    public bool ReadOnly { get; set; }
    //public string Editor => "Not yet implemented";
}