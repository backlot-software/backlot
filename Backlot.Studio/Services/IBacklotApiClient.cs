using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public interface IBacklotApiClient
{
    Uri BaseUrl { get; }
    
    Task<bool> IsAuthenticated();
    Task<object?> WhoAmI();

    Task<ApiEnvelope<StatusBody>?> Status();
    
    Task<ApiEnvelope<T>> Get<T>(string roleName, string scenario, CancellationToken ct = default);
    // PlayAsync — generic primitive mirroring the server convention api/role/{rolename}/{scenario}.
    // GET overload: uid is appended as the sole query param only when non-empty (director scenarios
    // pass none). POST overload: the body is serialized as JSON. PlayAllowingClientErrorAsync is the
    // POST variant that recovers a structured 4xx body instead of throwing (WR-02).
    Task<ApiEnvelope<T>> Get<T>(string roleName, string scenario, string uid, CancellationToken ct = default);

    Task<ApiEnvelope<TR>> Post<TB, TR>(string roleName, string scenario, TB body, CancellationToken ct = default)
        where TB : IRequestBody;

    // SendRawAsync — used by the Client tester page to send an arbitrary (method + path + body)
    // request through the authenticated pipeline and capture the raw response (status/body/timing)
    // without throwing on non-success statuses, so any outcome can be inspected.
    Task<RawApiResponse> SendRawAsync(string method, string path, string? body, CancellationToken ct = default);
}
