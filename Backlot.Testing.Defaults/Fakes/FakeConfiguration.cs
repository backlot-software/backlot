using System.Linq.Expressions;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Services;

namespace Backlot.Testing.Defaults.Fakes;

public class FakeConfiguration(IDictionary<string, string?> source) : IConfigurationManager
{
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
        return source.TryGetValue($"{fullname}.{propertyName}", out var val) ? val ?? string.Empty : string.Empty;
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
        return source.Keys.Where(x => x.StartsWith(path)).ToArray();
    }

    public void ResolveConfiguration(IScenario instance, string? named = null)
    {
        // todo: update in framework.
        // throw new NotImplementedException("Make sure Resolve Configuration works like BaseSettingsManager");
    }

    public Task Update(IConfigurationInfo configuration)
    {
        source.Add(configuration.Name, configuration.Value.ToString());
        return Task.CompletedTask;
    }
}