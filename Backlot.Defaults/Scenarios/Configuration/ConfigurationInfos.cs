using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.Defaults.Scenarios.Configuration;

[Scenario(typeof(ConfigurationInfos), access: [Access.Admin])]
public class ConfigurationInfos : DirectorScenario<ConfigurationInfos, IEnumerable<IConfigurationInfo>>
{
    private readonly IConfigurationManager _configuration;

    public ConfigurationInfos(IDirector role, 
        IConfigurationManager configuration) : base(role)
    {
        _configuration = configuration;
    }

    protected override IEnumerable<IConfigurationInfo> Exec()
    {
        return _configuration.GetAllSettings();
    }
}

