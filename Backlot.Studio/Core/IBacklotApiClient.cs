using Backlot.Studio.Core.Models.Request;
using Backlot.Studio.Core.Models.Response;

namespace Backlot.Studio.Core;

public interface IBacklotApiClient
{
    Uri BaseUrl { get; }
    
    Task<bool> IsAuthenticated();
    Task<object?> WhoAmI();

    Task<ApiEnvelope<StatusBody>?> Status();
    
    /// <summary>
    /// Play the specified scenario by the director.
    /// </summary>
    /// <param name="scenario"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<ApiEnvelope<T>> Play<T>(string scenario, CancellationToken ct = default);
    
    /// <summary>
    /// Play the specified senario using use the persisted entity stored with uniqueId uid as it's actor.
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="scenario"></param>
    /// <param name="uid"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<ApiEnvelope<T>> Play<T>(string roleName, string scenario, string uid, CancellationToken ct = default);

    /// <summary>
    /// Play the specified senario using the body as it's actor.
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="scenario"></param>
    /// <param name="body"></param>
    /// <param name="ct"></param>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="R"></typeparam>
    /// <returns></returns>
    Task<ApiEnvelope<R>> Play<B, R>(string roleName, string scenario, B body, CancellationToken ct = default)
        where B : IRequestBody;

    // SendRawAsync — used by the Client tester page to send an arbitrary (method + path + body)
    // request through the authenticated pipeline and capture the raw response (status/body/timing)
    // without throwing on non-success statuses, so any outcome can be inspected.
    Task<RawApiResponse> SendRawAsync(string method, string path, string? body, CancellationToken ct = default);
}
