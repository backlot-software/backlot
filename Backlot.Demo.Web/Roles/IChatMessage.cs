using Backlot.Core;

namespace Backlot.Demo.Web.Roles;

public interface IChatMessage : IRole
{
    string Role { get; set; }
    string Content { get; set; }
}
