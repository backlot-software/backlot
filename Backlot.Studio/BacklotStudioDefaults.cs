using Backlot.Authentication.Razor;

namespace Backlot.Studio;

/// <summary>
/// Names Backlot Studio claims inside a host application. Signing in is done by
/// Backlot.Authentication.Razor, see <see cref="BacklotSignInDefaults"/>.
/// </summary>
public static class BacklotStudioDefaults
{
    /// <summary>MVC area the Studio Razor Pages live in (<c>Areas/Studio/Pages</c>).</summary>
    public const string AreaName = "Studio";

    /// <summary>Cookie authentication scheme the Studio signs in with.</summary>
    [Obsolete("Use BacklotSignInDefaults.AuthenticationScheme.")]
    public const string AuthenticationScheme = BacklotSignInDefaults.AuthenticationScheme;

    /// <summary>Authorization policy applied to every Studio page.</summary>
    [Obsolete("Use BacklotSignInDefaults.AuthorizationPolicy.")]
    public const string AuthorizationPolicy = BacklotSignInDefaults.AuthorizationPolicy;
}
