using Microsoft.AspNetCore.Mvc;

namespace Backlot.Authentication.Razor;

/// <summary>
/// URL helpers for pages that use the Backlot sign-in.
/// </summary>
public static class BacklotSignInUrlHelperExtensions
{
    /// <summary>
    /// The login page (<see cref="BacklotSignInOptions.LoginPath"/>), returning to
    /// <paramref name="returnUrl"/> once signed in, e.g. <c>/studio/login?ReturnUrl=%2Fportal</c>.
    /// </summary>
    public static string BacklotLogin(this IUrlHelper urlHelper, string? returnUrl = null)
    {
        ArgumentNullException.ThrowIfNull(urlHelper);

        var options = urlHelper.ActionContext.HttpContext.RequestServices
            .GetRequiredService<BacklotSignInOptions>();

        // Routed through Content("~/...") so the host's own PathBase is honoured too.
        var login = urlHelper.Content("~" + options.EffectiveLoginPath);
        return string.IsNullOrEmpty(returnUrl)
            ? login
            : $"{login}{(login.Contains('?') ? '&' : '?')}ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
