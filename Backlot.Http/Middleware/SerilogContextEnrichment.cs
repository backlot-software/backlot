using System.Text.RegularExpressions;
using Backlot.Core.Security;
using Serilog;
using Serilog.Context;
// ReSharper disable ClassNeverInstantiated.Global

namespace Backlot.Http.Middleware;

public sealed class SerilogContextEnrichment : IMiddleware
{
    public async Task ExecuteAsync(MiddlewareContext request,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (LogContext.PushProperty("CtxUsername", MaskEmail(UserContext.Current)))
            using (LogContext.PushProperty("CtxRequestid", Guid.NewGuid().ToString("N")))
            using (LogContext.PushProperty("CtxSessionid", GetSessionId(request.Request.Message.Headers)))
            using (LogContext.PushProperty("CtxPath", request.Request.Message.RequestUri?.PathAndQuery))
            {
                await next();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "An unhandled exception is caught by SerilogContextWrapper");
            throw;
        }
    }

    private static string GetSessionId(System.Net.Http.Headers.HttpHeaders headers)
    {
        return headers.TryGetValues("x-session-id", out var values)
            ? values.FirstOrDefault() ?? "<unidentified>"
            : "<unidentified>";
    }

    private const string Pattern =
        @"(?<=[\w]{1})[\w-\._\+%\\]*(?=[\w]{1}@)|(?<=@[\w]{1})[\w-_\+%]*(?=\.)";

    private static string MaskEmail(IUserContext uctx)
    {
        if (!uctx.IsAuthenticated)
        {
            return "anonymous";
        }

        var s = uctx.UserName;

        if (string.IsNullOrWhiteSpace(s) || s.Length < 3)
            return "*";

        if (!s.Contains("@"))
            return $"{s.First()}{new string('*', s.Length - 2)}{s.Last()}";

        if (s.Split('@')[0].Length < 4)
            return "*@*.*";

        return Regex.Replace(s, Pattern, m => new string('*', m.Length));
    }
}