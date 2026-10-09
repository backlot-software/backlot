using Backlot.Core;

namespace Backlot.Demo.Web.Roles;

public interface IChat : IRole
{
    List<IChatMessage> Messages { get; set; }
}
