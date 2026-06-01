using System;

namespace Backlot.Core.Abstraction.Configuration;

/// <summary>
/// When marked as configurable this item can be managed by the <c>ISettingsManager</c>.
/// And maintained and maintained via "GetAllSettings"
/// </summary>
public class ConfigurableAttribute : Attribute
{
    //todo: public ConfigurableAttribute(string friendlyName, string editor)

}