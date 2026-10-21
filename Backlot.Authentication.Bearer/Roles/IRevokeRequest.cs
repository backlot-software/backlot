using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

public interface IRevokeRequest : IRole
{
    public string RefreshToken { get; set; }
}
