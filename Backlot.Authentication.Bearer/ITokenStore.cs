namespace Backlot.Authentication.Bearer;

/// <summary>
/// Tokens returned to the client by the Token scenario.
/// </summary>
public sealed record TokenSet(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");

/// <summary>
/// The user an access token belongs to.
/// </summary>
public sealed record TokenUser(string UserName, string[] Groups, IDictionary<string, string> Claims);

/// <summary>
/// Issues, validates, refreshes and revokes the tokens of this installation.
/// ----
/// The default <see cref="JwtTokenStore"/> is stateless (signed JWTs), which works on Azure Functions and behind load balancers.
/// Register your own implementation (e.g. database or Redis backed) to manage tokens and refresh tokens yourself.
/// No scenarios need to be implemented, the Token scenario and the BearerUserContext use this store.
/// </summary>
public interface ITokenStore
{
    /// <summary>Issue a new access and refresh token for an authenticated user.</summary>
    Task<TokenSet> Issue(string userName, string[] groups);

    /// <summary>Validate an access token. Returns null when the token is invalid, expired or revoked. Must never throw.</summary>
    Task<TokenUser?> ValidateAccess(string accessToken);

    /// <summary>Exchange a refresh token for a new token set. Returns null when the refresh token is invalid, expired or revoked.</summary>
    Task<TokenSet?> Refresh(string refreshToken);

    /// <summary>Revoke a refresh token (logout). Stateless stores may ignore this, their tokens expire by themselves.</summary>
    Task Revoke(string refreshToken);
}
