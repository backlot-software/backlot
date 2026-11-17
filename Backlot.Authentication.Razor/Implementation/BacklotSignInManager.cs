using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backlot.Authentication.Razor.Core;
using Backlot.Authentication.Razor.Core.Models;
using Backlot.Authentication.Razor.Core.Models.Request;
using Backlot.Authentication.Razor.Core.Models.Response;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;

namespace Backlot.Authentication.Razor.Implementation;

/// <summary>
/// Basic: the base64 "username:password" is validated with the IsAuthenticated scenario and kept in session.
/// Bearer (Backlot.Authentication.Bearer gateway flow):
/// 1. Start:    PKCE + state, gateway/authorize, send the browser to the identity provider.
/// 2. Callback: the identity provider returns to the API (GatewayCallbackUri = {api}/api/role/gateway/callback),
///              which redirects the browser to the done url.
/// 3. Complete: gateway/access exchanges the API's code, tokens are stored in session and the cookie is signed in.
/// </summary>
public class BacklotSignInManager : IBacklotSignInManager
{
    /// <summary>Reported when <c>BacklotSignIn:BaseUrl</c> is missing or malformed.</summary>
    public const string NotConfiguredMessage = "BaseUrl not configured correctly on host";

    private const string UnreachableMessage = "Could not reach the Backlot API. Try again.";
    private const string NoAccessMessage =
        "You are signed in at the identity provider, but this user has no access to the Backlot API.";

    private readonly IBacklotApiClient _apiClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BacklotSignInOptions _options;

    public BacklotSignInManager(IBacklotApiClient apiClient, IHttpContextAccessor httpContextAccessor,
        BacklotSignInOptions options)
    {
        _apiClient = apiClient;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    private HttpContext Context => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("Signing in requires an HTTP request.");

    private ISession Session => Context.Session;

    public async Task<bool> IsSignedInAsync()
    {
        // Authenticate explicitly: the Backlot scheme is never the host's default, so
        // HttpContext.User is not populated on anonymous pages.
        var result = await Context.AuthenticateAsync(BacklotSignInDefaults.AuthenticationScheme);
        return result.Succeeded;
    }

    public Task<string> GetApiAuthSchemeAsync() => _apiClient.AuthScheme();

    public async Task<BacklotSignInResult> SignInBasicAsync(string username, string password, string? returnUrl)
    {
        if (!_apiClient.IsConfigured) return BacklotSignInResult.Failed(NotConfiguredMessage);

        // Stored WITHOUT "Basic " prefix, and temporarily, so AuthHeaderHandler can inject it on the validation call.
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        Session.SetString(BacklotSignInDefaults.AuthSchemeSessionKey, AuthHeaderHandler.BasicScheme);
        Session.SetString(BacklotSignInDefaults.BasicAuthSessionKey, encoded);

        bool isValid;
        try
        {
            isValid = await _apiClient.IsAuthenticated();
        }
        catch (UnauthorizedAccessException)
        {
            // The API answers wrong credentials with a 401, which AuthHeaderHandler raises as this.
            isValid = false;
        }
        catch
        {
            Session.Remove(BacklotSignInDefaults.BasicAuthSessionKey);
            return BacklotSignInResult.Failed(UnreachableMessage);
        }

        if (!isValid)
        {
            Session.Remove(BacklotSignInDefaults.BasicAuthSessionKey);
            return BacklotSignInResult.Failed("Invalid username or password.");
        }

        await SignInCookie(username);
        return BacklotSignInResult.Success(ResolveReturnUrl(returnUrl));
    }

    public async Task<BacklotSignInResult> StartBearerAsync(string? returnUrl, string doneUrl)
    {
        if (!_apiClient.IsConfigured) return BacklotSignInResult.Failed(NotConfiguredMessage);

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

        Session.SetString(BacklotSignInDefaults.LoginFlowSessionKey,
            JsonSerializer.Serialize(new LoginFlow(verifier, state, returnUrl)));

        try
        {
            var location = (await _apiClient.Play<GatewayRequest, LocationBody>("gateway", "authorize",
                new GatewayRequest { RedirectUrl = doneUrl, State = state, CodeChallenge = challenge })).Body?.Location;

            return string.IsNullOrEmpty(location)
                ? BacklotSignInResult.Failed("The Backlot API did not return a location to sign in.")
                : BacklotSignInResult.Success(location);
        }
        catch (Exception ex)
        {
            return BacklotSignInResult.Failed(Reason(ex));
        }
    }

    public async Task<BacklotSignInResult> CompleteBearerAsync(string? code, string? state, string? error, string doneUrl)
    {
        var json = Session.GetString(BacklotSignInDefaults.LoginFlowSessionKey);
        Session.Remove(BacklotSignInDefaults.LoginFlowSessionKey);
        var flow = json is null ? null : JsonSerializer.Deserialize<LoginFlow>(json);

        if (flow is null || flow.State != state)
            return BacklotSignInResult.Failed("The sign in could not be verified. Try again.");

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            return BacklotSignInResult.Failed(error == "access_denied"
                ? NoAccessMessage
                : "The identity provider did not complete the sign in. Try again.");

        TokenSetBody tokens;
        try
        {
            tokens = (await _apiClient.Play<GatewayRequest, TokenSetBody>("gateway", "access",
                new GatewayRequest { Code = code, CodeVerifier = flow.Verifier, RedirectUrl = doneUrl })).Body!;
        }
        catch (Exception ex)
        {
            return BacklotSignInResult.Failed(Reason(ex));
        }

        Session.SetString(BacklotSignInDefaults.AuthSchemeSessionKey, AuthHeaderHandler.BearerScheme);
        Session.SetString(BacklotSignInDefaults.AccessTokenSessionKey, tokens.AccessToken);
        Session.SetString(BacklotSignInDefaults.RefreshTokenSessionKey, tokens.RefreshToken);

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
            Session.Remove(BacklotSignInDefaults.AuthSchemeSessionKey);
            Session.Remove(BacklotSignInDefaults.AccessTokenSessionKey);
            Session.Remove(BacklotSignInDefaults.RefreshTokenSessionKey);
            return BacklotSignInResult.Failed(Reason(ex));
        }

        await SignInCookie(userName ?? "Unknown");
        return BacklotSignInResult.Success(ResolveReturnUrl(flow.ReturnUrl));
    }

    public async Task SignOutAsync()
    {
        // Clear session first — destroys the API credential.
        if (Context.Features.Get<ISessionFeature>() is not null)
            Session.Clear();

        await Context.SignOutAsync(BacklotSignInDefaults.AuthenticationScheme);
    }

    public AuthenticationHeaderValue? GetApiAuthorization()
    {
        // HttpContext.Session throws outright unless the session middleware ran for this request.
        var context = _httpContextAccessor.HttpContext;
        var session = context?.Features.Get<ISessionFeature>() is not null ? context.Session : null;
        return AuthHeaderHandler.GetAuthorization(session);
    }

    private async Task SignInCookie(string userName)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName)], BacklotSignInDefaults.AuthenticationScheme);

        await Context.SignInAsync(
            BacklotSignInDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });
    }

    // Only local urls are followed, anything else lands on the default return url.
    private string ResolveReturnUrl(string? returnUrl) =>
        IsLocalUrl(returnUrl) ? returnUrl! : _options.EffectiveDefaultReturnUrl;

    // Same rules as IUrlHelper.IsLocalUrl: a rooted path, but not protocol relative ("//", "/\").
    private static bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) &&
        url[0] == '/' &&
        (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

    private static string Reason(Exception ex) => ex switch
    {
        UnauthorizedAccessException => NoAccessMessage,
        BacklotApiException { StatusCode: { } status } api =>
            $"Sign in failed ({(int)status}). {ApiMessage(api.ResponseBody)}",
        _ => UnreachableMessage
    };

    // The API's message when its envelope Body is a plain string, e.g. "The redirect uri is not allowed."
    private static string ApiMessage(string? responseBody)
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
