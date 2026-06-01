using System.Diagnostics;
using System.Net;

namespace Backlot.Http.Media;

public interface IMediaFormatter
{
    string MediaType { get; }

    Task<ResponseData> GetResponse<T>(
        RequestData request,
        T returnObj,
        Stopwatch stopwatch,
        HttpStatusCode statusCode=HttpStatusCode.OK);
}