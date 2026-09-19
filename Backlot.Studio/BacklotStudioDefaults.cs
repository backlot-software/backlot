namespace Backlot.Studio;

/// <summary>
/// Names Backlot Studio claims inside a host application. Every one of them is deliberately
/// Studio-specific: the package is mounted into somebody else's app, so it must never take over
/// the host's default authentication scheme, cookies or session.
/// </summary>
public static class BacklotStudioDefaults
{
    /// <summary>MVC area the Studio Razor Pages live in (<c>Areas/Studio/Pages</c>).</summary>
    public const string AreaName = "Studio";

    /// <summary>Cookie authentication scheme registered for the Studio. Never the host's default.</summary>
    public const string AuthenticationScheme = "BacklotStudio";

    /// <summary>Authorization policy applied to every Studio page except the login page.</summary>
    public const string AuthorizationPolicy = "BacklotStudio";

    /// <summary>Name of the Studio's authentication cookie.</summary>
    public const string AuthenticationCookieName = ".Backlot.Studio.Auth";

    /// <summary>Name of the Studio's session cookie.</summary>
    public const string SessionCookieName = ".Backlot.Studio.Session";

    /// <summary>Session key holding the base64 basic-auth credential forwarded to the Backlot API.</summary>
    internal const string BasicAuthSessionKey = "BasicAuthHeader";
}
