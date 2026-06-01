using System.Linq.Expressions;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Services;

namespace Backlot.Demo.Console;

public class DictionaryConfiguration : IConfigurationManager
{
    private readonly IDictionary<string, string?> _configuration;

    public DictionaryConfiguration(IDictionary<string, string> configuration)
    {
        _configuration = configuration;
    }

    public Task Delete(IConfigurationInfo configuration)
    {
        throw new NotImplementedException();
    }

    public string Get<T>(Expression<Func<T, string>> setting, string? named = null)
    {
        if (setting.Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException("selector is not a memberaccess expression type.");

        var propertyName = (setting.Body as MemberExpression)?.Member.Name;
        var fullname = string.IsNullOrEmpty(named) ? typeof(T).FullName : $"{typeof(T).FullName}.{named}";
        return _configuration.TryGetValue($"{fullname}.{propertyName}", out var val) ? val ?? string.Empty : string.Empty;
    }

    public IEnumerable<ConfigurationInfo> GetAllSettings()
    {
        throw new NotImplementedException();
    }

    public void ResolveConfiguration(IWatcher instance, string named = null)
    {
        throw new NotImplementedException();
    }

    public Stream GetContainerStream()
    {
        throw new NotImplementedException();
    }

    public string[] GetNames(string path)
    {
        throw new NotImplementedException();
    }

    public void ResolveConfiguration(IScenario instance, string? named = null)
    {
        // todo: throw new NotImplementedException();
    }

    public Task Update(IConfigurationInfo configuration)
    {
        throw new NotImplementedException();
    }

}