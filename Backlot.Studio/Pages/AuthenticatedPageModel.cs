using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Pages;

public abstract class AuthenticatedPageModel : PageModel
{
    /// <summary>
    /// Populates ViewData["Username"] from the authenticated cookie claim.
    /// Call this in every page handler that renders _Layout.cshtml to ensure
    /// the sidebar identity block is never blank (e.g., when the page is reached
    /// directly via a ReturnUrl bookmark rather than navigating from the dashboard).
    /// </summary>
    protected void SetUserContext()
    {
        ViewData["Username"] = User.Identity?.Name ?? "Unknown user";
    }

    protected async Task<(T? Value, IActionResult? Redirect)> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return (await apiCall(), null);
        }
        catch (Services.BacklotApiUnauthorizedException)
        {
            // Turbo-safe full-page redirect — not a frame-scoped redirect (T-02-04)
            Response.Headers["Turbo-Visit-Control"] = "reload";
            return (default, RedirectToPage("/Login"));
        }
    }
}
