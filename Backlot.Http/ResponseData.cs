using System.Net;

// ReSharper disable CollectionNeverQueried.Global

namespace Backlot.Http;

/// <summary>
/// Generic response data that can be use by specific http implementation to return a compatible response.
/// </summary>
public class ResponseData
{
    /// <summary>
    /// String representation of the content that need to be responded
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Headers that need to be set on the final response
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    public required HttpStatusCode StatusCode { get; init; }
}