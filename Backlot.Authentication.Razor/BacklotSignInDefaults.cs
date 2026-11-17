namespace Backlot.Authentication.Razor;

/// <summary>
/// Names the Backlot sign-in claims inside a host application. The scheme and policy are never the
/// host's defaults: the host opts its own pages in through <see cref="AuthorizationPolicy"/>.
/// </summary>
public static class BacklotSignInDefaults
{
    /// <summary>MVC area the login pages live in (<c>Areas/BacklotSignIn/Pages</c>).</summary>
    public const string AreaName = "BacklotSignIn";

    /// <summary>Cookie authentication scheme of the Backlot sign-in. Never the host's default.</summary>
    public const string AuthenticationScheme = "Backlot";

    /// <summary>
    /// Authorization policy requiring a Backlot sign-in. Apply it to the host's own pages, e.g.
    /// <c>[Authorize(Policy = BacklotSignInDefaults.AuthorizationPolicy)]</c>.
    /// </summary>
    public const string AuthorizationPolicy = "Backlot";

    /// <summary>Name of the authentication cookie, shared by every page of the host.</summary>
    public const string AuthenticationCookieName = ".Backlot.Auth";

    /// <summary>Name of the session cookie, shared by every page of the host.</summary>
    public const string SessionCookieName = ".Backlot.Session";

    /// <summary>Session key holding the base64 basic-auth credential forwarded to the Backlot API.</summary>
    internal const string BasicAuthSessionKey = "BasicAuthHeader";

    /// <summary>Session key holding the scheme ("Basic" or "Bearer") used towards the Backlot API.</summary>
    internal const string AuthSchemeSessionKey = "AuthScheme";

    /// <summary>Session key holding the Bearer access token forwarded to the Backlot API.</summary>
    internal const string AccessTokenSessionKey = "AccessToken";

    /// <summary>Session key holding the Bearer refresh token, used to renew the access token.</summary>
    internal const string RefreshTokenSessionKey = "RefreshToken";

    /// <summary>Session key holding the PKCE code verifier and state while signing in with the identity provider.</summary>
    internal const string LoginFlowSessionKey = "LoginFlow";
}
