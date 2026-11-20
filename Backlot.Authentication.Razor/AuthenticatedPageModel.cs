using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Authentication.Razor;

/// <summary>
/// Base class for Razor Pages that call the Backlot API as the signed-in user, in Backlot Studio
/// and in the host's own pages alike.
/// </summary>
public abstract class AuthenticatedPageModel : PageModel
{
    /// <summary>
    /// Populates ViewData["Username"] from the authenticated cookie claim.
    /// Call this in every page handler that renders a layout showing the user, so the identity block
    /// is never blank (e.g., when the page is reached directly via a ReturnUrl bookmark).
    /// </summary>
    protected void SetUserContext()
    {
        ViewData["Username"] = User.Identity?.Name ?? "Unknown user";
    }

    /// <summary>
    /// Signs the user out of the API and the host, e.g. when the API rejects the stored credential.
    /// </summary>
    protected Task SignOutAsync() =>
        HttpContext.RequestServices.GetRequiredService<IBacklotSignInManager>().SignOutAsync();

    /// <summary>
    /// Executes an API call safely by handling unauthorized exceptions and returning the result or a redirect.
    /// </summary>
    /// <param name="apiCall">The asynchronous function representing the API call to execute.</param>
    /// <typeparam name="T">The type of the value returned by the API call.</typeparam>
    /// <returns>A tuple containing the result of the API call and an optional redirect action.</returns>
    protected async Task<(T? Value, IActionResult? Redirect)> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return (await apiCall(), null);
        }
        catch (UnauthorizedAccessException)
        {
            // The API no longer accepts the credential while the cookie is still valid. Signed out
            // first, otherwise the login page would see the cookie and send the user straight back.
            await SignOutAsync();

            // Turbo-safe full-page redirect — not a frame-scoped redirect (T-02-04)
            Response.Headers["Turbo-Visit-Control"] = "reload";
            return (default, Redirect(Url.BacklotLogin(Request.PathBase + Request.Path + Request.QueryString)));
        }
    }
}
