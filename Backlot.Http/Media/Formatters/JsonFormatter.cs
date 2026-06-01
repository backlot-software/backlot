using System.Diagnostics;
using System.Net;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;

namespace Backlot.Http.Media.Formatters
{
    public class JsonFormatter : IMediaFormatter
    {
        public string MediaType => "application/json";

        public Task<ResponseData> GetResponse<T>(RequestData req, T returnObj, Stopwatch stopwatch,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                var jsnResponse = (int)statusCode < 400
                    ? new JsonResponse<T>(returnObj, stopwatch.ElapsedMilliseconds)
                    {
                        Status = Status.OK
                    }
                    : new JsonResponse<T>(returnObj, stopwatch.ElapsedMilliseconds)
                    {
                        Status = Status.FAIL
                    };

                var response = new ResponseData
                {
                    StatusCode = statusCode,
                    Content = jsnResponse.ToJson(Strategy.SerializeForInteraction)
                };
                
                response.Headers.Add("Content-Type", MediaType);
            
                return Task.FromResult(response);
            }
            catch (Exception exception)
            {
                return Task.FromException<ResponseData>(exception);
            }
        }
    }
}
