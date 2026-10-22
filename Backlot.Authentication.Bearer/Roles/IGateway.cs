using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

/// <summary>
/// The authorization code flow (PKCE, S256 only), played in three steps:
/// 1. Authorize: RedirectUrl, State and CodeChallenge are required.
/// 2. Callback (gateway mode only): Code and State are required.
/// 3. Access: Code, CodeVerifier and RedirectUrl are required.
/// Each scenario validates the properties it needs.
/// </summary>
public interface IGateway : IRole
{
    /// <summary>
    /// Authorize: client url the browser is sent back to with ?code=..&amp;state=.. after login (must be in AllowedRedirectUris).
    /// Access: the same value as sent with Authorize; the code is bound to it.
    /// </summary>
    public string RedirectUrl { get; set; }

    /// <summary>
    /// Authorize: random value created by the client, returned unchanged on the RedirectUrl.
    /// The client compares it to protect against cross-site request forgery.
    /// Callback: the state returned by the external identity provider (?state=..). This is the signed state Backlot created
    /// in Authorize, it carries the client's RedirectUrl, State and CodeChallenge.
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Authorize: PKCE base64url(SHA256(code_verifier)). The client keeps the code_verifier secret
    /// and sends it with Access, so only this client can redeem the code.
    /// </summary>
    public string CodeChallenge { get; set; }

    /// <summary>
    /// Callback: authorization code issued by the external identity provider (?code=.. on the GatewayCallbackUri).
    /// Backlot exchanges it server side for the provider's id_token; it is not a Backlot code.
    /// Access: Backlot authorization code the browser received on the RedirectUrl (?code=..). Valid for 2 minutes.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Access: PKCE, the secret the CodeChallenge of the Authorize request was created from.
    /// </summary>
    public string CodeVerifier { get; set; }
}
