using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

/// <summary>
/// Credentials posted by the internal login page, together with the authorize request.
/// </summary>
public interface ILoginRequest : IRole
{
    public string UserName { get; set; }
    public string Password { get; set; }

    public string RedirectUri { get; set; }
    public string State { get; set; }
    public string CodeChallenge { get; set; }
}
