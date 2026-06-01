using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Backlot.Http;
using Backlot.Http.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backlot.WebApp;

public static class HttpExtensions
{
    /// <summary>
    /// Get HttpRequestMessage from HttpRequest.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static HttpRequestMessage Message(this HttpRequest request)
    {
        var requestMessage = new HttpRequestMessage(
            new HttpMethod(request.Method),
            $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}");

        foreach (var header in request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
        }
        
        return requestMessage;
    }
    
    public static async Task<IResult> GetResultContent<T>(
        this IMediaFormatResolver resolver, 
        HttpRequest req, 
        T returnObj,
        Stopwatch stopwatch, 
        ILogger logger,
        HttpStatusCode code = HttpStatusCode.OK)
    {
        var mediaResponse = await resolver.GetMediaResponseData(new RequestData
        {
            Message = req.Message(),
            Body = req.Body,
        }, returnObj, stopwatch, logger, code);

        // 1. Get the Content-Type from the dictionary (defaulting if not found)
        mediaResponse.Headers.TryGetValue("Content-Type", out var contentType);
    
        // 2. Add all other headers to the response
        foreach (var header in mediaResponse.Headers.Where(h => h.Key != "Content-Type"))
        {
            // Skip Content-Type if you want Results.Content to handle it, 
            // or just add it here and let Results.Content overwrite it.
            req.HttpContext.Response.Headers[header.Key] = header.Value;
        }

        // 3. Return the Result
        return Results.Content(
            content: mediaResponse.Content,
            contentType: contentType ?? "application/json",
            statusCode: (int)mediaResponse.StatusCode);
    }
}