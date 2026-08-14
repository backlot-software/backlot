using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Backlot.Studio.Services;

// CRITICAL: Uses IHttpContextAccessor (not a scoped session) to avoid ObjectDisposedException
// under load (T-02-05). Session is read inside SendAsync only — never in the constructor.
public class BasicAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BasicAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Both guards matter when the Studio is embedded in a host app: there is no HttpContext on a
        // background call, and HttpContext.Session throws outright unless the session middleware ran
        // for this request (MapBacklotStudio only branches it onto the Studio's own path).
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Features.Get<ISessionFeature>() is not null)
        {
            var basicAuthHeader = httpContext.Session.GetString(BacklotStudioDefaults.BasicAuthSessionKey);

            if (!string.IsNullOrEmpty(basicAuthHeader))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", basicAuthHeader);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new BacklotApiUnauthorizedException();
        }

        return response;
    }
}
