using System.Net;
using Backlot.Http;
using Backlot.Http.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Backlot.Functions;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Wrap Backlot.Http.Middleware into an Azure Functions Middleware compatible layer.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class FunctionsHttpMiddleware<T> : 
    IFunctionsWorkerMiddleware
    where T : IMiddleware, new()
{
    private readonly IMiddleware _wrapper = new T();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Items.TryGetValue(nameof(MiddlewareContext), out var item) && item is MiddlewareContext middlewareContext)
        {
            await _wrapper.ExecuteAsync(middlewareContext, () => next(context), context.CancellationToken);
            return;
        }

        middlewareContext = await ToGenericRequest(context);
        context.Items[nameof(MiddlewareContext)] = middlewareContext;

        await _wrapper.ExecuteAsync(middlewareContext, () => next(context), context.CancellationToken);
    }

    private static async Task<MiddlewareContext> ToGenericRequest(FunctionContext ctx)
    {
        var req = await ctx.GetHttpRequestDataAsync();

        if (req is null)
            throw new ArgumentNullException(nameof(req));
        
        return new MiddlewareContext
        {
            Request = new RequestData
            {
                Message = req.Message(),
                Body = req.Body
            },
            CurrentInstanceServices = () => ctx.InstanceServices,
            ChangeInstanceServices = s => ctx.InstanceServices = s,
            HttpResponseStatus = () => ctx.GetHttpResponseData()!.StatusCode,
            InvokeResult = SetAzureResponse
        };

        async Task SetAzureResponse(string m, HttpStatusCode sc)
        {
            var res = req.CreateResponse();
            res.StatusCode = sc;
            await res.WriteStringAsync(m);
            ctx.GetInvocationResult().Value = res;
        }
    }
}