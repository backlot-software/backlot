using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Backlot.Studio.Core;
using Backlot.Studio.Core.Models;
using Backlot.Studio.Core.Models.Request;
using Backlot.Studio.Core.Models.Response;
using Backlot.Studio.Implementation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Areas.Studio.Pages;

/// <summary>
/// Sign in through the identity provider, for a Backlot API using Bearer authentication
/// (Backlot.Authentication.Bearer gateway flow):
/// 1. Start:    PKCE + state, gateway/authorize, send the browser to the identity provider.
/// 2. Callback: the identity provider returns to the API (GatewayCallbackUri = {api}/api/role/gateway/callback),
///              which redirects the browser to Done.
/// 3. Done:     the API returns here (redirectUrl, must be in the API's AllowedRedirectUris), gateway/access,
///              tokens are stored in session and the Studio cookie is signed in.
/// </summary>
[AllowAnonymous]
public class LoginBearerModel : PageModel
{
    private const string NotConfiguredMessage = "BaseUrl not configured correctly on host";

    private readonly IBacklotApiClient _apiClient;

    public LoginBearerModel(IBacklotApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [FromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>The url the API returns to with its code; add it to AllowedRedirectUris on the API.</summary>
    public string DoneUrl => Url.Page("/LoginBearer", "done", null, Request.Scheme)!;

    private string DashboardUrl => Url.Page("/Index") ?? "/";

    public IActionResult OnGet()
    {
        if (!_apiClient.IsConfigured)
            ModelState.AddModelError(string.Empty, NotConfiguredMessage);

        return Page();
    }

    // 1. create state + PKCE (kept in session), ask the API for the identity provider's login url.
    public async Task<IActionResult> OnGetStartAsync()
    {
        if (!_apiClient.IsConfigured) return OnGet();

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

        HttpContext.Session.SetString(BacklotStudioDefaults.LoginFlowSessionKey,
            JsonSerializer.Serialize(new LoginFlow(verifier, state, ReturnUrl)));

        return await Continue(new GatewayRequest { RedirectUrl = DoneUrl, State = state, CodeChallenge = challenge },
            "authorize", location => Redirect(location));
    }

    // 3. the API returns here with its own code. This request comes from a cross-site redirect chain (identity
    //    provider, API), so the SameSite=Strict Studio cookies are not sent: the browser is moved on by a page
    //    instead of a redirect, that navigation is same-site and carries the session again.
    public IActionResult OnGetDone(string? code, string? state, string? error)
    {
        var location = Url.Page("/LoginBearer", "Exchange", new { code, state, error })!;
        return Content(
            $"<!doctype html><meta http-equiv=\"refresh\" content=\"0;url={HtmlEncoder.Default.Encode(location)}\">Signing in…",
            "text/html");
    }

    // exchange the API's code for tokens.
    public async Task<IActionResult> OnGetExchangeAsync(string? code, string? state, string? error)
    {
        var json = HttpContext.Session.GetString(BacklotStudioDefaults.LoginFlowSessionKey);
        HttpContext.Session.Remove(BacklotStudioDefaults.LoginFlowSessionKey);
        var flow = json is null ? null : JsonSerializer.Deserialize<LoginFlow>(json);

        if (flow is null || flow.State != state)
        {
            ModelState.AddModelError(string.Empty, "The sign in could not be verified. Try again.");
            return Page();
        }

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            ModelState.AddModelError(string.Empty, error == "access_denied"
                ? "You are signed in at the identity provider, but this user has no access to the Backlot API."
                : "The identity provider did not complete the sign in. Try again.");
            return Page();
        }

        TokenSetBody tokens;
        try
        {
            tokens = (await _apiClient.Play<GatewayRequest, TokenSetBody>("gateway", "access",
                new GatewayRequest { Code = code, CodeVerifier = flow.Verifier, RedirectUrl = DoneUrl })).Body!;
        }
        catch (Exception ex)
        {
            return Failed(ex);
        }

        HttpContext.Session.SetString(BacklotStudioDefaults.AuthSchemeSessionKey, AuthHeaderHandler.BearerScheme);
        HttpContext.Session.SetString(BacklotStudioDefaults.AccessTokenSessionKey, tokens.AccessToken);
        HttpContext.Session.SetString(BacklotStudioDefaults.RefreshTokenSessionKey, tokens.RefreshToken);

        string? userName;
        try
        {
            userName = (await _apiClient.WhoAmI()) is JsonElement whoAmI &&
                       whoAmI.TryGetProperty("UserName", out var name)
                ? name.GetString()
                : null;
        }
        catch (Exception ex)
        {
            // the API rejected the user, do not leave a half signed in session behind.
            HttpContext.Session.Remove(BacklotStudioDefaults.AuthSchemeSessionKey);
            HttpContext.Session.Remove(BacklotStudioDefaults.AccessTokenSessionKey);
            HttpContext.Session.Remove(BacklotStudioDefaults.RefreshTokenSessionKey);
            return Failed(ex);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName ?? "Unknown")], BacklotStudioDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            BacklotStudioDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        return LocalRedirect(Url.IsLocalUrl(flow.ReturnUrl) ? flow.ReturnUrl! : DashboardUrl);
    }

    // Plays a gateway scenario and continues to the location it returns.
    private async Task<IActionResult> Continue(GatewayRequest request, string scenario, Func<string, IActionResult> next)
    {
        try
        {
            var location = (await _apiClient.Play<GatewayRequest, LocationBody>("gateway", scenario, request)).Body?.Location;
            if (!string.IsNullOrEmpty(location)) return next(location);

            ModelState.AddModelError(string.Empty, "The Backlot API did not return a location to sign in.");
            return Page();
        }
        catch (Exception ex)
        {
            return Failed(ex);
        }
    }

    private IActionResult Failed(Exception ex)
    {
        ModelState.AddModelError(string.Empty, ex switch
        {
            UnauthorizedAccessException =>
                "You are signed in at the identity provider, but this user has no access to the Backlot API.",
            BacklotApiException { StatusCode: { } status } api =>
                $"Sign in failed ({(int)status}). {Reason(api.ResponseBody)}",
            _ => "Could not reach the Backlot API. Try again."
        });
        return Page();
    }

    // The API's message when its envelope Body is a plain string, e.g. "The redirect uri is not allowed."
    private static string Reason(string? responseBody)
    {
        try
        {
            return responseBody is not null &&
                   JsonDocument.Parse(responseBody).RootElement.TryGetProperty("Body", out var body) &&
                   body.ValueKind == JsonValueKind.String
                ? body.GetString()!
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record LoginFlow(string Verifier, string State, string? ReturnUrl);
}
