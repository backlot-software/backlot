using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Core.Services;

public abstract class BaseSettingsManager : IConfigurationManager
{
    /// <summary>
    /// The environment for which the json file needed to be loaded
    /// We use the "{Environment}.jsonsettings.json" pattern.
    /// </summary>
    protected string Environment { get; set; }
    
    public abstract IEnumerable<ConfigurationInfo> GetAllSettings();
    
    public abstract string[] GetNames(string path);

    private void ResolveConfigurationObj(object instance, string named = null)
    {
        // configurable properties need to be public and having a getter and a setter.
        var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props.Where(p => p.GetCustomAttributes(false).OfType<ConfigurableAttribute>().Any()))
        {
            var fullname = string.IsNullOrEmpty(named) ? instance.GetType().NamespaceName() : $"{instance.GetType().NamespaceName()}.{named}";
            var value = Get($"{fullname}.{prop.Name}");

            if (!(prop.PropertyType == typeof(string) 
                  || prop.PropertyType == typeof(int) || 
                  prop.PropertyType == typeof(decimal) || 
                  prop.PropertyType == typeof(float) ||
                  prop.PropertyType == typeof(bool)))
                throw new NotImplementedException(
                    "Configurable property types other than bool, string or numbers are not supported yet.");

            if (!string.IsNullOrEmpty(value))
            {
                var cvalue = prop.PropertyType == typeof(string)
                    ? value
                    : Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture);
                prop.SetValue(instance, cvalue);
            }
        }
    }
    
    
    /// <summary>
    /// Get value using a lambda expression
    /// </summary>
    /// <param name="setting"></param>
    /// <param name="named"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public string Get<T>(Expression<Func<T, string>> setting, string named = null)
    {
        return Get(GetStringPath(setting, named)) ?? string.Empty;
    }

    /// <summary>
    /// Get value using a string path, this is the central function all "Get" value calls are executed when implementing a BaseSettingsManager.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public abstract string Get(string path);
    
    public virtual void ResolveConfiguration(IScenario instance, string named = null)
    {
        ResolveConfigurationObj(instance, named);
    }

    public virtual void ResolveConfiguration(IWatcher instance, string named = null)
    {
        ResolveConfigurationObj(instance, named);
    }
    
    public abstract Stream GetContainerStream();
    public abstract Task Update(IConfigurationInfo configuration);
    public abstract Task Delete(IConfigurationInfo configuration);
    
    
    private string GetStringPath<T>(Expression<Func<T, string>> setting, string named = null)
    {
        if (setting.Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("selector is not a memberaccess expression type.");

        var propertyName = (setting.Body as MemberExpression)?.Member.Name;
        var fullname = string.IsNullOrEmpty(named) ? typeof(T).FullName : $"{typeof(T).FullName}.{named}";
        return $"{fullname}.{propertyName}";
    }
}