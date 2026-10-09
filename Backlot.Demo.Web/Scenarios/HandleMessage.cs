using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Demo.Web.Roles;
using Backlot.Demo.Web.Services;

namespace Backlot.Demo.Web.Scenarios;

[Scenario(typeof(HandleMessage), access: [Access.Open])]
public class HandleMessage(IChat role, ICodeGenAgentService agents) : Scenario<IChat, string>(role)
{
    protected override async Task<string> ExecAsync()
    {
        var latest = Role.Messages?.LastOrDefault()?.Content ?? string.Empty;
        var agent = latest.Contains("extract roles", StringComparison.OrdinalIgnoreCase)
            ? CodeGenAgent.RoleExtraction
            : latest.Contains("generate lowcode", StringComparison.OrdinalIgnoreCase)
                ? CodeGenAgent.LowCode
                : CodeGenAgent.Chat;

        return await agents.SendAsync(Role.Messages ?? [], agent);
    }
}
