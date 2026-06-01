using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.Defaults.Scenarios.Configuration;

[Scenario(typeof(TryUpdateConfiguration), access: [Access.Admin])]
public class TryUpdateConfiguration(
    IConfigurationInfo role,
    IConfigurationManager configuration)
    : Scenario<TryUpdateConfiguration, IConfigurationInfo, bool>(role)
{
    protected override async Task<bool> ExecAsync()
    {
        try
        {
            await configuration.Update(Role);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

