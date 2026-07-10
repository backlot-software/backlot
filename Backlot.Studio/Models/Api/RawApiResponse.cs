namespace Backlot.Studio.Models.Api;

// Result of an arbitrary (method + path + body) request sent through the authenticated
// Backlot API pipeline by the Client tester page. Unlike PlayAsync it never throws on a
// non-success status — the raw status, reason and body are surfaced to the operator so the
// response can be inspected regardless of outcome.
public class RawApiResponse
{
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public bool IsSuccess { get; set; }
}
