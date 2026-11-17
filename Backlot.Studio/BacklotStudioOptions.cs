namespace Backlot.Studio;

/// <summary>
/// Configuration for the embedded Backlot Studio UI.
/// </summary>
/// <remarks>
/// The same instance is registered as a singleton and shared by
/// <see cref="BacklotStudioExtensions.AddBacklotStudio(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{BacklotStudioOptions}?, System.Action{Backlot.Authentication.Razor.BacklotSignInOptions}?)"/>
/// and <see cref="BacklotStudioExtensions.MapBacklotStudio"/>, which is what lets the mount path be
/// supplied at either call site. Everything derived from <see cref="PathPrefix"/> is read lazily
/// (on the first request), so <c>MapBacklotStudio("/admin")</c> still wins over the configured value.
/// Signing in (the API address, cookies, session, idle timeout) is configured on
/// <see cref="Backlot.Authentication.Razor.BacklotSignInOptions"/>.
/// </remarks>
public sealed class BacklotStudioOptions
{
    /// <summary>Configuration section bound by the <c>IConfiguration</c> overload of <c>AddBacklotStudio</c>.</summary>
    public const string SectionName = "BacklotStudio";

    private string _pathPrefix = "/studio";

    /// <summary>
    /// Path the Studio is mounted on, e.g. <c>/studio</c>. Normalised to a leading slash without a
    /// trailing one; an empty value (or <c>"/"</c>) mounts the Studio at the application root.
    /// </summary>
    public string PathPrefix
    {
        get => _pathPrefix;
        set => _pathPrefix = NormalizePrefix(value);
    }

    /// <summary>Request path the embedded <c>wwwroot</c> is served from, e.g. <c>/studio/assets</c>.</summary>
    public string AssetRequestPath => PathPrefix + "/assets";

    /// <summary>Route-template form of <see cref="PathPrefix"/> (no leading slash), e.g. <c>studio</c>.</summary>
    internal string RouteTemplatePrefix => PathPrefix.TrimStart('/');

    private static string NormalizePrefix(string? value)
    {
        var trimmed = value?.Trim().Trim('/');
        return string.IsNullOrEmpty(trimmed) ? string.Empty : "/" + trimmed;
    }
}
