using System.Diagnostics;
using System.Net;
using Backlot.Http;
using Backlot.Http.Media;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backlot.Functions;

/// <summary>
/// Backlot.Http compatibility wrappers.
/// </summary>
public static class BacklotHttpExtensions
{
    public static HttpRequestMessage Message(this HttpRequestData req)
    {
        var message = new HttpRequestMessage(new HttpMethod(req.Method), req.Url);

        foreach (var header in req.Headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return message;
    }

    public static async Task<HttpResponseData> GetHttpResponseData<T>(
        this IMediaFormatResolver resolver, 
        HttpRequestData req, 
        T returnObj,
        Stopwatch stopwatch,
        ILogger logger,
        HttpStatusCode code = HttpStatusCode.OK)
    {
        var mediaResponse = await resolver.GetMediaResponseData(new RequestData
        {
            Message = req.Message(),
            Body = req.Body
        }, returnObj, stopwatch, logger, code);
        
        var response = req.CreateResponse(mediaResponse.StatusCode);
        
        foreach (var header in mediaResponse.Headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        await response.WriteStringAsync(mediaResponse.Content);
        
        return response;
    }
}