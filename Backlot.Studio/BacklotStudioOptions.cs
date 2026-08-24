using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Backlot.Studio;

/// <summary>
/// Configuration for the embedded Backlot Studio UI.
/// </summary>
/// <remarks>
/// The same instance is registered as a singleton and shared by
/// <see cref="BacklotStudioExtensions.AddBacklotStudio(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{BacklotStudioOptions}?)"/>
/// and <see cref="BacklotStudioExtensions.MapBacklotStudio"/>, which is what lets the mount path be
/// supplied at either call site. Everything derived from <see cref="PathPrefix"/> is read lazily
/// (on the first request), so <c>MapBacklotStudio("/admin")</c> still wins over the configured value.
/// </remarks>
public sealed class BacklotStudioOptions
{
    /// <summary>Configuration section bound by the <c>IConfiguration</c> overload of <c>AddBacklotStudio</c>.</summary>
    public const string SectionName = "BacklotStudio";

    private string _pathPrefix = "/studio";

    /// <summary>
    /// Absolute base address of the Backlot HTTP API the Studio talks to.
    /// </summary>
    /// <remarks>
    /// Leave empty -- the default -- when the Studio is co-hosted with the API, which is the case for
    /// any host built with <c>BuildWebApp</c>. The address is then resolved per request from the
    /// server's own listening addresses, so nothing has to be configured for a local run, a different
    /// port or a container. Set it only to point the Studio at an API in another process, in which
    /// case it must be an absolute URI.
    /// </remarks>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path the Studio is mounted on, e.g. <c>/studio</c>. Normalised to a leading slash without a
    /// trailing one; an empty value (or <c>"/"</c>) mounts the Studio at the application root.
    /// </summary>
    public string PathPrefix
    {
        get => _pathPrefix;
        set => _pathPrefix = NormalizePrefix(value);
    }

    /// <summary>
    /// How long an idle operator stays signed in. Applied to both the session and the auth cookie
    /// so the two can never expire out of step.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Secure-flag policy for the Studio's cookies. Defaults to <see cref="CookieSecurePolicy.Always"/>;
    /// relax it to <see cref="CookieSecurePolicy.SameAsRequest"/> to sign in over plain HTTP locally.
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.Always;

    /// <summary>Escape hatch to adjust the Studio's cookie authentication after the defaults are applied.</summary>
    public Action<CookieAuthenticationOptions>? ConfigureCookie { get; set; }

    /// <summary>Escape hatch to adjust the Studio's session after the defaults are applied.</summary>
    public Action<SessionOptions>? ConfigureSession { get; set; }

    /// <summary>Request path the embedded <c>wwwroot</c> is served from, e.g. <c>/studio/assets</c>.</summary>
    public string AssetRequestPath => PathPrefix + "/assets";

    /// <summary>Cookie path scoping the Studio's cookies to its own mount point.</summary>
    internal string CookiePath => PathPrefix.Length == 0 ? "/" : PathPrefix;

    /// <summary>Route-template form of <see cref="PathPrefix"/> (no leading slash), e.g. <c>studio</c>.</summary>
    internal string RouteTemplatePrefix => PathPrefix.TrimStart('/');

    /// <summary><see cref="PathString"/> form of <see cref="PathPrefix"/>; empty when mounted at the root.</summary>
    internal PathString PathBase => PathPrefix.Length == 0 ? PathString.Empty : new PathString(PathPrefix);

    private static string NormalizePrefix(string? value)
    {
        var trimmed = value?.Trim().Trim('/');
        return string.IsNullOrEmpty(trimmed) ? string.Empty : "/" + trimmed;
    }
}
