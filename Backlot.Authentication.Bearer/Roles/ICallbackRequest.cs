using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

/// <summary>
/// Code and state the external identity provider returned to the login page (gateway mode).
/// </summary>
public interface ICallbackRequest : IRole
{
    public string Code { get; set; }
    public string State { get; set; }
}
