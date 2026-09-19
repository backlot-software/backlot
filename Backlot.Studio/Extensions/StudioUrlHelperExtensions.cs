using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Extensions;

/// <summary>
/// URL helpers for the Studio views.
/// </summary>
public static class StudioUrlHelperExtensions
{
    /// <summary>
    /// Resolves a file in the Studio's embedded <c>wwwroot</c> to an absolute path, e.g.
    /// <c>Url.StudioAsset("css/studio.css")</c> becomes <c>/studio/assets/css/studio.css</c>.
    /// </summary>
    /// <remarks>
    /// Views cannot use <c>~/css/studio.css</c>: that resolves against the host application's root,
    /// not the Studio's mount path, which is only known at runtime.
    /// </remarks>
    public static string StudioAsset(this IUrlHelper urlHelper, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(urlHelper);
        ArgumentNullException.ThrowIfNull(relativePath);

        var options = urlHelper.ActionContext.HttpContext.RequestServices
            .GetRequiredService<BacklotStudioOptions>();

        // Routed through Content("~/...") so the host's own PathBase is honoured too.
        return urlHelper.Content($"~{options.AssetRequestPath}/{relativePath.TrimStart('/')}");
    }
}
