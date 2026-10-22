using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

public interface ITokenRequest : IRole
{
    public string RefreshToken { get; set; }
}
