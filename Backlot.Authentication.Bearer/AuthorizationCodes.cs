using System.Security.Cryptography;
using System.Text;
using Backlot.Http;
using Microsoft.IdentityModel.Tokens;

namespace Backlot.Authentication.Bearer;

/// <summary>
/// Stateless authorization codes (PKCE, S256) and gateway state, both signed with the installation SigningKey.
/// </summary>
public sealed class AuthorizationCodes(BearerOptions options)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly SignedTokens _tokens = new(options);

    /// <summary>
    /// Validates the client part of an authorize request. Throws a BadRequestException when invalid.
    /// </summary>
    public void ValidateRequest(string redirectUri, string codeChallenge)
    {
        if (string.IsNullOrWhiteSpace(redirectUri) ||
            !options.AllowedRedirectUris.Contains(redirectUri, StringComparer.Ordinal))
            throw new BadRequestException("The redirect uri is not allowed.");

        // base64url(SHA256) is always 43 characters.
        if (string.IsNullOrWhiteSpace(codeChallenge) || codeChallenge.Length != 43)
            throw new BadRequestException("A S256 code challenge is required.");
    }

    public string IssueCode(string userName, string[] groups, string redirectUri, string codeChallenge)
    {
        return _tokens.Create(SignedTokens.CodeType, CodeLifetime, new Dictionary<string, object>
        {
            ["sub"] = userName,
            ["groups"] = groups,
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = codeChallenge
        });
    }

    /// <summary>
    /// Where the browser is sent after a successful login.
    /// </summary>
    public static string RedirectLocation(string redirectUri, string code, string state)
    {
        var separator = redirectUri.Contains('?') ? '&' : '?';
        return $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state ?? string.Empty)}";
    }

    /// <summary>
    /// Returns the user of a valid code, when the redirect uri matches and the code verifier fits the code challenge.
    /// </summary>
    public async Task<(string userName, string[] groups)?> Redeem(string code, string codeVerifier, string redirectUri)
    {
        var identity = await _tokens.Validate(code, SignedTokens.CodeType);
        if (identity == null || string.IsNullOrWhiteSpace(codeVerifier)) return null;

        if (identity.FindFirst("redirect_uri")?.Value != redirectUri) return null;

        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(challenge),
                Encoding.ASCII.GetBytes(identity.FindFirst("code_challenge")?.Value ?? string.Empty)))
            return null;

        var userName = identity.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userName)) return null;

        return (userName, identity.FindAll("groups").Select(c => c.Value).ToArray());
    }

    /// <summary>
    /// Packs the client authorize request into the state sent to the external identity provider.
    /// </summary>
    public string CreateGatewayState(string redirectUri, string state, string codeChallenge)
    {
        return _tokens.Create(SignedTokens.StateType, StateLifetime, new Dictionary<string, object>
        {
            ["redirect_uri"] = redirectUri,
            ["state"] = state ?? string.Empty,
            ["code_challenge"] = codeChallenge
        });
    }

    public async Task<(string redirectUri, string state, string codeChallenge)?> ReadGatewayState(string gatewayState)
    {
        var identity = await _tokens.Validate(gatewayState, SignedTokens.StateType);
        if (identity == null) return null;

        return (identity.FindFirst("redirect_uri")?.Value ?? string.Empty,
            identity.FindFirst("state")?.Value ?? string.Empty,
            identity.FindFirst("code_challenge")?.Value ?? string.Empty);
    }
}
