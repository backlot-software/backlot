using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Revokes a refresh token (logout). Whether this has effect depends on the registered ITokenStore.
/// </summary>
[Scenario(typeof(Revoke), access: [Core.Security.Access.Open])]
public class Revoke(ITokenRequest role, ITokenStore tokenStore)
    : Scenario<Revoke, ITokenRequest, bool>(role)
{
    protected override bool PersistAndRelate => false;

    protected override async Task<bool> ExecAsync()
    {
        await tokenStore.Revoke(Role.RefreshToken);
        return true;
    }
}
