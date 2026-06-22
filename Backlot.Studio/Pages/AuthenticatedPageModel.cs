using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Pages;

public abstract class AuthenticatedPageModel : PageModel
{
    protected async Task<T?> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return await apiCall();
        }
        catch (Services.BacklotApiUnauthorizedException)
        {
            // Turbo-safe full-page redirect — not a frame-scoped redirect (T-02-04)
            Response.Headers["Turbo-Visit-Control"] = "reload";
            Response.Redirect("/login");
            return default;
        }
    }
}
