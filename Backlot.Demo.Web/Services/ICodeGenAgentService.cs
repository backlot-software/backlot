using Backlot.Demo.Web.Roles;

namespace Backlot.Demo.Web.Services;

public interface ICodeGenAgentService
{
    Task<string> SendAsync(IReadOnlyCollection<IChatMessage> messages, CodeGenAgent agent);
}
