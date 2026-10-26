using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;

namespace Backlot.Demo.Web.Pages;

/// <summary>
/// Simplest example client of the Backlot.Authentication.Bearer gateway flow:
/// 1. Start:    POST gateway/authorize, send the browser to the identity provider.
/// 2. Callback: the identity provider returns to Backlot (GatewayCallbackUri = /api/role/gateway/callback), which redirects here.
/// 3. Done:     Backlot returns here (redirectUrl), POST gateway/access for the tokens and call WhoAmI with the Bearer token.
/// </summary>
public class LoginModel : PageModel
{
    private const string FlowCookie = "demo.login.flow";
    private static readonly HttpClient Http = new();

    public string? Error { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? WhoAmI { get; private set; }

    private string Api => $"{Request.Scheme}://{Request.Host}/api/role";
    private string DoneUrl => $"{Request.Scheme}://{Request.Host}/login?handler=done";

    public void OnGet()
    {
    }

    // 1. create state + PKCE, ask Backlot for the identity provider's login url.
    public async Task<IActionResult> OnGetStartAsync()
    {
        var verifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16));
        
        // the client keeps verifier and state until the browser returns.
        Response.Cookies.Append(FlowCookie, $"{verifier}|{state}", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        var body = await Play("gateway/authorize", new { RedirectUrl = DoneUrl, State = state, CodeChallenge = challenge });
        return body is null ? Page() : Redirect(Get(body.Value, "Location")!);
    }

    // 3. Backlot returns here with its own code, exchange it for tokens.
    public async Task<IActionResult> OnGetDoneAsync(string code, string state, string? error)
    {
        var flow = Request.Cookies[FlowCookie]?.Split('|');
        Response.Cookies.Delete(FlowCookie);

        if (flow is not { Length: 2 } || flow[1] != state)
        {
            Error = "State does not match, login aborted.";
            return Page();
        }

        if (!string.IsNullOrEmpty(error))
        {
            Error = $"Login failed: {error}.";
            return Page();
        }

        var body = await Play("gateway/access", new { Code = code, CodeVerifier = flow[0], RedirectUrl = DoneUrl });
        if (body is null) return Page();

        AccessToken = Get(body.Value, "AccessToken");
        RefreshToken = Get(body.Value, "RefreshToken");

        // use the access token as Bearer.
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Api}/director/whoami");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        WhoAmI = await (await Http.SendAsync(request)).Content.ReadAsStringAsync();

        return Page();
    }

    /// <summary>
    /// POST a role to a Backlot scenario and return the Body of the response, or null (Error is set).
    /// </summary>
    private async Task<JsonElement?> Play(string roleAndScenario, object role)
    {
        // Backlot roles are PascalCase; StringContent also sends a Content-Length, which Backlot needs to read the body.
        var content = new StringContent(JsonSerializer.Serialize(role), Encoding.UTF8, "application/json");
        var response = await Http.PostAsync($"{Api}/{roleAndScenario}", content);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var body = json.TryGetProperty("Body", out var b) ? b : json;

        if (response.IsSuccessStatusCode) return body;

        Error = $"{roleAndScenario}: {(int)response.StatusCode} {body}";
        return null;
    }

    private static string? Get(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.GetString() : null;
}
