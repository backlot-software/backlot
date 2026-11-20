using System.Net;
using System.Net.Http.Headers;
using Backlot.Authentication.Razor.Core.Models.Request;
using Backlot.Authentication.Razor.Core.Models.Response;
using Microsoft.AspNetCore.Http.Features;

namespace Backlot.Authentication.Razor.Implementation;

// Adds the Authorization header to every Backlot API call, from the signed-in user's session:
// - "Basic": the base64 credential stored by BacklotSignInManager.SignInBasicAsync.
// - "Bearer": the access token stored by BacklotSignInManager.CompleteBearerAsync. On a 401 the refresh token is
//   exchanged once (tokenrequest/refresh scenario) and the request is retried.
// CRITICAL: Uses IHttpContextAccessor (not a scoped session) to avoid ObjectDisposedException
// under load (T-02-05). Session is read inside SendAsync only — never in the constructor.
public class AuthHeaderHandler : DelegatingHandler
{
    public const string BasicScheme = "Basic";
    public const string BearerScheme = "Bearer";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Both guards matter: there is no HttpContext on a background call, and HttpContext.Session
        // throws outright unless the session middleware ran for this request.
        var session = _httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>() is not null
            ? _httpContextAccessor.HttpContext.Session
            : null;

        var scheme = session?.GetString(BacklotSignInDefaults.AuthSchemeSessionKey) ?? BasicScheme;

        // Buffered so the request can be sent a second time after a token refresh.
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        SetAuthorization(request, session, scheme);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            scheme == BearerScheme &&
            session is not null &&
            await TryRefresh(request.RequestUri!, session, cancellationToken))
        {
            response.Dispose();
            using var retry = Clone(request, body);
            SetAuthorization(retry, session, scheme);
            response = await base.SendAsync(retry, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        return response;
    }

    private static void SetAuthorization(HttpRequestMessage request, ISession? session, string scheme)
    {
        request.Headers.Authorization = GetAuthorization(session, scheme);
    }

    /// <summary>The Authorization header stored in <paramref name="session"/>, or <c>null</c> when nobody signed in.</summary>
    internal static AuthenticationHeaderValue? GetAuthorization(ISession? session) =>
        GetAuthorization(session, session?.GetString(BacklotSignInDefaults.AuthSchemeSessionKey) ?? BasicScheme);

    private static AuthenticationHeaderValue? GetAuthorization(ISession? session, string scheme)
    {
        var token = session?.GetString(scheme == BearerScheme
            ? BacklotSignInDefaults.AccessTokenSessionKey
            : BacklotSignInDefaults.BasicAuthSessionKey);

        return string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue(scheme, token);
    }

    // Exchanges the refresh token for a new token set and stores it in the session.
    private async Task<bool> TryRefresh(Uri requestUri, ISession session, CancellationToken cancellationToken)
    {
        var refreshToken = session.GetString(BacklotSignInDefaults.RefreshTokenSessionKey);
        if (string.IsNullOrEmpty(refreshToken)) return false;

        // Same API as the failed request: every Backlot scenario lives under {BaseUrl}/api/role/.
        var path = requestUri.AbsolutePath;
        var apiRoot = path[..(path.IndexOf("/api/role/", StringComparison.OrdinalIgnoreCase) + 1)];
        var refreshUri = new Uri(requestUri, $"{apiRoot}api/role/refreshtoken/refresh");

        using var refresh = new HttpRequestMessage(HttpMethod.Post, refreshUri)
        {
            Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = refreshToken })
        };
        // Backlot needs a Content-Length to read the body.
        await refresh.Content.LoadIntoBufferAsync(cancellationToken);

        using var response = await base.SendAsync(refresh, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TokenSetBody>>(cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(envelope?.Body?.AccessToken)) return false;

        session.SetString(BacklotSignInDefaults.AccessTokenSessionKey, envelope.Body.AccessToken);
        session.SetString(BacklotSignInDefaults.RefreshTokenSessionKey, envelope.Body.RefreshToken);
        return true;
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request, byte[]? body)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content!.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
