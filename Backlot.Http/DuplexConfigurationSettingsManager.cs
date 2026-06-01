using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Services;
using Microsoft.Extensions.Configuration;

namespace Backlot.Http;

/// <summary>
/// The Duplex Configuration Settings manager is a facade for reading from the actual settings manager but when not available try to find it within the azure configuration.
/// This is a readonly approach, the leading configuration manager is the only one that can update the settings.
/// </summary>
public class DuplexConfigurationSettingsManager(IConfiguration azureConfiguration, BaseSettingsManager settingsManager)
    : BaseSettingsManager
{
    #region duplex
    // all functions using both the leading and the azure configuration. this contains all "R" (READ) related operations.

    public override string Get(string path)
    {
        var str = settingsManager.Get(path);
        if (string.IsNullOrEmpty(str)) // when not found in leading configuration manager, try to get from azure configuration.
        {
            str = azureConfiguration[path];
            if (string.IsNullOrEmpty(str)) //when not found the full (named) path, try to find one level below
            {
                var parts = path.Split('.');
                // try to fallback on the default value, means building a new path without the "named" part in it. The namepart is always the second last part of the path.
                var noneNamedPath = string.Join('.', parts.Take(parts.Length - 2).Concat([parts.Last()]));
                str = azureConfiguration[noneNamedPath];
            }
        }

        return str;
    }
    
    #endregion

    
    #region leading only
    // All functions only using the leading manager this contains all "CUD" related operation (NOT READ)
    
    public override IEnumerable<ConfigurationInfo> GetAllSettings()
    {
        //all settings are the same for every configurationmanager.
        return settingsManager.GetAllSettings();
    }

    public override string[] GetNames(string path)
    {
        return settingsManager.GetNames(path);
    }
    
    public override Stream GetContainerStream()
    {
        return settingsManager.GetContainerStream();
    }

    public override async Task Update(IConfigurationInfo configuration)
    {
        await settingsManager.Update(configuration);
    }

    public override async Task Delete(IConfigurationInfo configuration)
    {
        await settingsManager.Delete(configuration);
    }
    
    #endregion
}