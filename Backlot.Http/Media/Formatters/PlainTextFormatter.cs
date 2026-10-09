using System.Diagnostics;
using System.Net;
using System.Text;

namespace Backlot.Http.Media.Formatters;

public class PlainTextFormatter : IMediaFormatter
{
    public string MediaType => "text/plain";

    public Task<ResponseData> GetResponse<T>(
        RequestData request,
        T returnObj,
        Stopwatch stopwatch,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        try
        {
            var content = returnObj switch
            {
                null => string.Empty,
                string str => str,
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                _ => returnObj.ToString() ?? string.Empty
            };

            var response = new ResponseData
            {
                StatusCode = statusCode,
                Content = content
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
