using System.Net.Http.Headers;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Security;

namespace Backlot.Http.Middleware;

public sealed class AuthenticationInitializer : IMiddleware
{
    public async Task ExecuteAsync(MiddlewareContext request,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {
        var userContext = ServiceLocator.Get<IUserContext>();

        if (TryGetSchemeToken(request.Request.Message.Headers, userContext.AuthScheme, out var token))
        {
            userContext.Token = token;
        }

        await next();
    }

    private static bool TryGetSchemeToken(HttpHeaders headers, string scheme, out string? token)
    {
        token = null;

        if (!headers.TryGetValues("Authorization", out var values))
        {
            return false;
        }

        var authHeaderValue = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeaderValue))
        {
            return false;
        }

        if (!AuthenticationHeaderValue.TryParse(authHeaderValue, out var parsed))
        {
            return false;
        }

        if (!scheme.Equals(parsed.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid authentication scheme '{parsed.Scheme}'", nameof(scheme));
        }

        token = parsed.Parameter?.Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}