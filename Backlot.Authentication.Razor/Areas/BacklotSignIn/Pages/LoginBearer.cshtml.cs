using System.Text.Encodings.Web;
using Backlot.Authentication.Razor.Core;
using Backlot.Authentication.Razor.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Authentication.Razor.Areas.BacklotSignIn.Pages;

/// <summary>
/// Sign in through the identity provider, for a Backlot API using Bearer authentication; the flow
/// itself is <see cref="BacklotSignInManager"/>. The identity provider returns to the API, which
/// redirects the browser to <see cref="DoneUrl"/> (must be in the API's AllowedRedirectUris).
/// </summary>
[AllowAnonymous]
public class LoginBearerModel : PageModel
{
    private readonly IBacklotApiClient _apiClient;
    private readonly IBacklotSignInManager _signInManager;

    public LoginBearerModel(IBacklotApiClient apiClient, IBacklotSignInManager signInManager)
    {
        _apiClient = apiClient;
        _signInManager = signInManager;
    }

    [FromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>The url the API returns to with its code; add it to AllowedRedirectUris on the API.</summary>
    public string DoneUrl => Url.Page("/LoginBearer", "done", new { area = BacklotSignInDefaults.AreaName }, Request.Scheme)!;

    public IActionResult OnGet()
    {
        if (!_apiClient.IsConfigured)
            ModelState.AddModelError(string.Empty, BacklotSignInManager.NotConfiguredMessage);

        return Page();
    }

    // 1. ask the API for the identity provider's login url and send the browser there.
    public async Task<IActionResult> OnGetStartAsync()
    {
        var result = await _signInManager.StartBearerAsync(ReturnUrl, DoneUrl);
        return Continue(result, location => Redirect(location));
    }

    // 3. the API returns here with its own code. This request comes from a cross-site redirect chain (identity
    //    provider, API), so the SameSite=Strict cookies are not sent: the browser is moved on by a page
    //    instead of a redirect, that navigation is same-site and carries the session again.
    public IActionResult OnGetDone(string? code, string? state, string? error)
    {
        var location = Url.Page("/LoginBearer", "Exchange",
            new { area = BacklotSignInDefaults.AreaName, code, state, error })!;
        return Content(
            $"<!doctype html><meta http-equiv=\"refresh\" content=\"0;url={HtmlEncoder.Default.Encode(location)}\">Signing in…",
            "text/html");
    }

    // exchange the API's code for tokens.
    public async Task<IActionResult> OnGetExchangeAsync(string? code, string? state, string? error)
    {
        var result = await _signInManager.CompleteBearerAsync(code, state, error, DoneUrl);
        return Continue(result, location => LocalRedirect(location));
    }

    private IActionResult Continue(BacklotSignInResult result, Func<string, IActionResult> next)
    {
        if (result.Succeeded) return next(result.RedirectUrl!);

        ModelState.AddModelError(string.Empty, result.Error!);
        return Page();
    }
}
