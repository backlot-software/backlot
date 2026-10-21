using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Http;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Exchanges an authorization code (with PKCE code verifier) or a refresh token for a new token set.
/// </summary>
[Scenario(typeof(Token), access: [Access.Open])]
public class Token(ITokenRequest role, ITokenStore tokenStore)
    : Scenario<Token, ITokenRequest, TokenSet>(role)
{
    protected override bool PersistAndRelate => false;

    protected override async Task<TokenSet> ExecAsync()
    {
        switch (Role.GrantType)
        {
            case "authorization_code":
                var user = await new AuthorizationCodes(BearerOptions.FromConfiguration())
                               .Redeem(Role.Code, Role.CodeVerifier, Role.RedirectUri)
                           ?? throw new UnauthorizedAccessException("Invalid authorization code.");
                return await tokenStore.Issue(user.userName, user.groups);

            case "refresh_token":
                return await tokenStore.Refresh(Role.RefreshToken)
                       ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            default:
                throw new BadRequestException("GrantType must be 'authorization_code' or 'refresh_token'.");
        }
    }
}
