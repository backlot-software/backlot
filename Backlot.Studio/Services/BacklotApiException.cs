using System.Net;

namespace Backlot.Studio.Services;

/// <summary>
/// Thrown when the Backlot API returns a non-success status. Unlike the bare
/// <see cref="HttpRequestException"/> produced by <c>EnsureSuccessStatusCode()</c>, this
/// carries the status code AND the response body so operators get the API's own diagnostic
/// detail (Backlot envelopes carry <c>Status</c>/diagnostic text in the body). See WR-05.
/// </summary>
public class BacklotApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseBody { get; }

    public BacklotApiException(HttpStatusCode statusCode, string? responseBody)
        : base($"The Backlot API returned {(int)statusCode} {statusCode}." +
               (string.IsNullOrWhiteSpace(responseBody) ? string.Empty : $" Body: {responseBody}"))
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
