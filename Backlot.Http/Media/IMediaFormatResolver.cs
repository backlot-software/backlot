using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Backlot.Http.Media;

/// <summary>
/// Resolve the formatter returning a response in the format of impplementation
/// </summary>
public interface IMediaFormatResolver
{
    /// <summary>
    /// Create a Media Response which can be used to set up implementation-specific responses (AspNetCore, Azure Functions, etc)
    /// </summary>
    /// <param name="request">Http Request data</param>
    /// <param name="returnObj">The object to be used for response</param>
    /// <param name="stopwatch">an optional stopwatch to add performance data to your results</param>
    /// <param name="logger"></param>
    /// <param name="statusCode">The http status code for the response to be build.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<ResponseData> GetMediaResponseData<T>(
        RequestData request,
        T returnObj, Stopwatch stopwatch,
        ILogger logger,
        HttpStatusCode statusCode = HttpStatusCode.OK);
}