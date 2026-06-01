using System.Net;
using Backlot.Core.Json;

namespace Backlot.Http.Middleware;

public sealed class Defender : IMiddleware
{
    public async Task ExecuteAsync(MiddlewareContext context,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {

        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            var attacked = false;
            
            var message = $"The request body is not accepted because it contains; ";
            
            if (requestBody.Contains(Meta.__Skills))
            {
                message += Meta.__Skills;
                attacked = true;
            }
            
            // Permission is allowed with special treatment since 10/10/2024. 
            // for future reference: you can add a check for __Permission here to check for "injection" and "attack" patterns
            
            if (attacked)
            {
                await context.InvokeResult(message, HttpStatusCode.BadRequest);
                return;
            }
        }

        if(context.Request.Body.CanSeek)
            context.Request.Body.Position = 0;


        await next();
    }
}