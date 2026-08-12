using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

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
        // Check if we're in an ASP.NET Core context before accessing session
        var hasHttpContext = _httpContextAccessor.HttpContext != null;
    
        // if (hasHttpContext)
        // {
        //     var session = _httpContextAccessor.HttpContext.Session;
        //     if (session != null)
        //     {
        //         await session.LoadAsync(cancellationToken);
        //     }
        // }

        // Only try to get auth header if we have a valid context
        if (hasHttpContext)
        {
            var basicAuthHeader = _httpContextAccessor.HttpContext?.Session.GetString("BasicAuthHeader");
        
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
