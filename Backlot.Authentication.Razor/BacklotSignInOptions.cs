using Microsoft.AspNetCore.Authentication.Cookies;

namespace Backlot.Authentication.Razor;

/// <summary>
/// Configuration for signing in to the Backlot API, shared by Backlot Studio and the host's own pages.
/// </summary>
/// <remarks>
/// The same instance is registered as a singleton and is read lazily (on the first request), so a
/// path given to <c>MapBacklotSignIn</c> or <c>MapBacklotStudio</c> still wins over the configured value.
/// </remarks>
public sealed class BacklotSignInOptions
{
    /// <summary>Configuration section bound by the <c>IConfiguration</c> overload of <c>AddBacklotSignIn</c>.</summary>
    public const string SectionName = "BacklotSignIn";

    private const string StandalonePathPrefix = "/account";

    private string? _pathPrefix;
    private string? _loginPath;
    private string? _defaultReturnUrl;

    /// <summary>
    /// Absolute base address of the Backlot HTTP API users sign in to. Required.
    /// </summary>
    /// <remarks>
    /// There is no fallback to the server's own listening addresses. When it is missing or not an
    /// absolute URI the host still starts, but the login page shows an alert saying so.
    /// </remarks>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="BaseUrl"/> as an absolute <see cref="Uri"/>, or <c>null</c> when it is missing or
    /// not an absolute URI. The single place the configured value is interpreted.
    /// </summary>
    public Uri? BaseUri =>
        !string.IsNullOrWhiteSpace(BaseUrl) && Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            ? uri
            : null;

    /// <summary>
    /// Path the login pages are mounted on (<c>{PathPrefix}/login</c>, <c>{PathPrefix}/login/bearer</c>,
    /// <c>{PathPrefix}/logout</c>). When <c>null</c> it defaults to the Studio's mount path when
    /// Backlot Studio is registered, so its sign-in URLs stay as they were, and to <c>/account</c> otherwise.
    /// </summary>
    public string? PathPrefix
    {
        get => _pathPrefix;
        set => _pathPrefix = value is null ? null : NormalizePrefix(value);
    }

    /// <summary>
    /// Local path users are sent to when they need to sign in. When <c>null</c> it is the built-in
    /// login page, <c>{PathPrefix}/login</c>; point it at a page of the host to use its own login page.
    /// </summary>
    public string? LoginPath
    {
        get => _loginPath;
        set => _loginPath = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Local URL users land on after signing in without a return URL. When <c>null</c> it is the
    /// Studio's dashboard when Backlot Studio is registered, and <c>/</c> otherwise.
    /// </summary>
    public string? DefaultReturnUrl
    {
        get => _defaultReturnUrl;
        set => _defaultReturnUrl = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// How long an idle user stays signed in. Applied to both the session and the auth cookie
    /// so the two can never expire out of step.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Secure-flag policy for the sign-in cookies. Defaults to <see cref="CookieSecurePolicy.Always"/>;
    /// relax it to <see cref="CookieSecurePolicy.SameAsRequest"/> to sign in over plain HTTP locally.
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.Always;

    /// <summary>Escape hatch to adjust the cookie authentication after the defaults are applied.</summary>
    public Action<CookieAuthenticationOptions>? ConfigureCookie { get; set; }

    /// <summary>Escape hatch to adjust the session after the defaults are applied.</summary>
    public Action<SessionOptions>? ConfigureSession { get; set; }

    /// <summary>Supplies <see cref="PathPrefix"/> when it is not set explicitly; used by Backlot Studio.</summary>
    internal Func<string>? PathPrefixFallback { get; set; }

    /// <summary>Supplies <see cref="DefaultReturnUrl"/> when it is not set explicitly; used by Backlot Studio.</summary>
    internal Func<string>? DefaultReturnUrlFallback { get; set; }

    // The values in effect. Kept apart from the settable properties on purpose: the configuration
    // binder writes every getter's value back through its setter, which would turn a computed
    // default into an explicit one before the Studio's fallback is known.

    /// <summary><see cref="PathPrefix"/> in effect.</summary>
    internal string EffectivePathPrefix => _pathPrefix ?? PathPrefixFallback?.Invoke() ?? StandalonePathPrefix;

    /// <summary><see cref="LoginPath"/> in effect.</summary>
    internal string EffectiveLoginPath => _loginPath ?? EffectivePathPrefix + "/login";

    /// <summary><see cref="DefaultReturnUrl"/> in effect.</summary>
    internal string EffectiveDefaultReturnUrl => _defaultReturnUrl ?? DefaultReturnUrlFallback?.Invoke() ?? "/";

    /// <summary>Route-template form of <see cref="EffectivePathPrefix"/> (no leading slash), e.g. <c>studio</c>.</summary>
    internal string RouteTemplatePrefix => EffectivePathPrefix.TrimStart('/');

    private static string NormalizePrefix(string value)
    {
        var trimmed = value.Trim().Trim('/');
        return trimmed.Length == 0 ? string.Empty : "/" + trimmed;
    }
}
