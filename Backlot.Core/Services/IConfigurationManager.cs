using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Core.Services;

/// <summary>
/// Settings Manager to load backlot (scenario) settings
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Get all settings for setting up an installation
    /// </summary>
    /// <returns>A dictionary with keynames and the corresponding type</returns>
    IEnumerable<ConfigurationInfo> GetAllSettings();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting">The specified property you like to get the setting for</param>
    /// <param name="named">If more configurations are available give the name of the specific configuration.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    string Get<T>(Expression<Func<T, string>> setting, string named=null);

    /// <summary>
    /// Get the named configurations of the given scenario/path.
    /// Named configuration can override the defaults of a scenario configuration.
    /// This implementation returns the defined named configurations
    /// Named configurations are valid ScenarioReferences
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string[] GetNames(string path);
    

    /// <summary>
    /// Resolve all Configurable marked properties of a scenario
    /// </summary>
    /// <param name="instance">The actual instance to resolve</param>
    /// <param name="named">If more configurations are available give the name of the specific configuration.</param>
    void ResolveConfiguration(IScenario instance, string named=null);
    
    /// <summary>
    /// Resolve all Configurable marked properties of a scenario
    /// </summary>
    /// <param name="instance">The actual instance to resolve</param>
    /// <param name="named">If more configurations are available give the name of the specific configuration.</param>
    void ResolveConfiguration(IWatcher instance, string named=null);

    /// <summary>
    /// JsonStream of the Container
    /// </summary>
    /// <returns></returns>
    Stream GetContainerStream();

    //void Update<T>(Expression<Func<T, string>> setting, object value, string named=null);
    Task Update(IConfigurationInfo configuration);
    Task Delete(IConfigurationInfo configuration);
}