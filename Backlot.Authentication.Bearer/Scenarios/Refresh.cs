using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Exchanges a refresh token for a new token set.
/// </summary>
[Scenario(typeof(Refresh), access: [Core.Security.Access.Open])]
public class Refresh(ITokenRequest role, ITokenStore tokenStore)
    : Scenario<Refresh, ITokenRequest, TokenSet>(role)
{
    protected override bool PersistAndRelate => false;

    protected override async Task<TokenSet> ExecAsync()
    {
        return await tokenStore.Refresh(Role.RefreshToken)
               ?? throw new UnauthorizedAccessException("Invalid refresh token.");
    }
}
