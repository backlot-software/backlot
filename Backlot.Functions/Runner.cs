using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;

namespace Backlot.Functions;

public class Runner
{

    
    [Function("status")]
    public static HttpResponseData Status([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            
        var backlotversion = typeof(Core.IDirector).Assembly.GetName().Version;
            
        response.WriteString(JObject.FromObject(new
        {
            TimeInMs = stopwatch.ElapsedMilliseconds,
            Body = $"Backlot - version {backlotversion}.",
            Status = HttpStatusCode.OK,
            ExcecutionTime = DateTimeOffset.Now
        }).ToString());
            
        return response;
    }
}


