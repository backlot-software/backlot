using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Services;

namespace Backlot.Testing.Core;

public class ConfigStub : IConfigurationManager
{
    private readonly IDictionary<string, string> _configuration;

    public ConfigStub(IDictionary<string, string> configuration)
    {
        _configuration = configuration;
    }
    
    public string Environment => "console";

    public Task Delete(IConfigurationInfo configuration)
    {
        return Task.CompletedTask;
    }

    public string Get<T>(Expression<Func<T, string>> setting, string? named = null)
    {
        if (setting.Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("selector is not a memberaccess expression type.");

        var propertyName = (setting.Body as MemberExpression)?.Member.Name;
        var fullname = string.IsNullOrEmpty(named) ? typeof(T).FullName : $"{typeof(T).FullName}.{named}";
        return _configuration[$"{fullname}.{propertyName}"] ?? string.Empty;
    }

    public IEnumerable<ConfigurationInfo> GetAllSettings()
    {
        return [];
    }

    public void ResolveConfiguration(IWatcher instance, string named = null)
    {
        
    }

    public Stream GetContainerStream()
    {
        return default(Stream);
    }

    public string[] GetNames(string path)
    {
        return [];
    }

    public void ResolveConfiguration(IScenario instance, string? named = null)
    {

    }

    public Task Update(IConfigurationInfo configuration)
    {
        return Task.CompletedTask;
    }

}