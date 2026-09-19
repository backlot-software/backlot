using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Areas.Studio.Pages;

/// <summary>
/// Base PageModel for Turbo-driven write forms. Isolates the phase's central hazard:
/// Turbo Drive follows a redirect after a form POST only on HTTP 303 (See Other) —
/// RedirectToPage/Redirect default to 302, which Turbo treats as a non-advance and will
/// NOT navigate (the form appears to hang). Likewise, Turbo swaps the page body on a form
/// submit only when the response status is 4xx/5xx; a plain Page() returns 200, which Turbo
/// treats as success and discards the re-rendered error body. See RESEARCH.md Pattern 1.
/// </summary>
public abstract class TurboEditPageModel : AuthenticatedPageModel
{
    /// <summary>303 See-Other so Turbo Drive follows the redirect via GET after a POST.</summary>
    protected IActionResult TurboRedirect(string url)
    {
        Response.StatusCode = (int)HttpStatusCode.SeeOther; // 303
        Response.Headers.Location = url;
        return new EmptyResult();
    }

    /// <summary>422 Unprocessable Entity so Turbo swaps the body with the re-rendered form.</summary>
    protected IActionResult TurboInvalidPage()
    {
        Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity; // 422
        return Page();
    }
}
