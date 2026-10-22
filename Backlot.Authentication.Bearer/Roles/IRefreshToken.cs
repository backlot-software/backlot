using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

public interface IRefreshToken : IRole
{
    public string RefreshToken { get; set; }
}
