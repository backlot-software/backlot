using System.Net;

namespace Backlot.Http.Middleware;

/// <summary>
/// Context used to execute Backlot middleware
///  can be wrapped by implementation-specific wrappers like:
/// - FunctionsHttpMiddleware
/// - AspNetCoreHttpMiddleware
/// </summary>
public class MiddlewareContext
{
    public required RequestData Request { get; init; }
    
    /// <summary>
    /// The middleware used IServiceProvider
    /// </summary>
    public required Func<IServiceProvider> CurrentInstanceServices { get; init; }
    public required Action<IServiceProvider> ChangeInstanceServices { get; init; }
    
    /// <summary>
    /// The current response status
    /// </summary>
    public required Func<HttpStatusCode> HttpResponseStatus { get; init; }
    
    /// <summary>
    /// Add a message and a status code to the result / response
    /// </summary>
    public required Func<string, HttpStatusCode, Task> InvokeResult { get; init; }
}