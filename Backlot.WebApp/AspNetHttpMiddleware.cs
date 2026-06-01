using System.Net;
using System.Threading.Tasks;
using Backlot.Http;
using Backlot.Http.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using IMiddleware = Backlot.Http.Middleware.IMiddleware;

namespace Backlot.WebApp;

/// <summary>
/// Wrap IMiddleware into a Aspnet core compatible layer.
/// </summary>
/// <param name="next"></param>
/// <typeparam name="T"></typeparam>
public sealed class AspNetMiddleware<T>(RequestDelegate next, ILogger<AspNetMiddleware<T>> logger)
    where T : IMiddleware, new()
{
    private readonly IMiddleware _middleware = new T();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(MiddlewareContext), out var item) && item is MiddlewareContext middlewareContext)
        {
            await _middleware.ExecuteAsync(
                middlewareContext,
                () => next(context),
                context.RequestAborted);
            return;
        }

        middlewareContext = new MiddlewareContext
        {
            Request = new RequestData
            {
                Message = context.Request.Message(),
                Body = context.Request.Body
            },
            CurrentInstanceServices = () => context.RequestServices,
            ChangeInstanceServices = s => context.RequestServices = s,
            HttpResponseStatus = () => (HttpStatusCode)context.Response.StatusCode,
            InvokeResult = async (message, statusCode) =>
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(message);
            }
        };

        context.Items[nameof(MiddlewareContext)] = middlewareContext;

        await _middleware.ExecuteAsync(
            middlewareContext,
            () => next(context),
            context.RequestAborted);
    }
}