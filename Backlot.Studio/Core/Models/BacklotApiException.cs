using System.Net;

namespace Backlot.Studio.Core.Models;

/// <summary>
/// Thrown when the Backlot API returns a non-success status. Unlike the bare
/// <see cref="HttpRequestException"/> produced by <c>EnsureSuccessStatusCode()</c>, this
/// carries the status code AND the response body so operators get the API's own diagnostic
/// detail (Backlot envelopes carry <c>Status</c>/diagnostic text in the body). See WR-05.
///
/// Derives from <see cref="HttpRequestException"/> so existing
/// <c>catch (HttpRequestException)</c> handlers across the pages continue to handle non-success
/// responses unchanged; handlers that need the status/body (e.g. the Edit save flow's 403
/// branch, WR-03) can catch <see cref="BacklotApiException"/> specifically.
/// </summary>
public class BacklotApiException : HttpRequestException
{
    public string? ResponseBody { get; }

    public BacklotApiException(HttpStatusCode statusCode, string? responseBody)
        : base($"The Backlot API returned {(int)statusCode} {statusCode}." +
               (string.IsNullOrWhiteSpace(responseBody) ? string.Empty : $" Body: {responseBody}"),
               inner: null,
               statusCode: statusCode)
    {
        ResponseBody = responseBody;
    }
}
