namespace Backlot.Http;

/// <summary>
/// Request data format which can be used by Backlot.Http for Middleware-context as well as resolving media formats.
/// </summary>
public class RequestData
{
    public required HttpRequestMessage Message { get; init; }
    
    /// <summary>
    /// Request body
    /// </summary>
    public required Stream Body { get; init; }
}