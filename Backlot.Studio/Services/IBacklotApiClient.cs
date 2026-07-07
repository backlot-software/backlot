using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public interface IBacklotApiClient
{
    Uri BaseUrl { get; }
    
    Task<bool> IsAuthenticatedAsync();
    Task<object?> WhoAmIAsync();

    Task<ApiEnvelope<StatusBody>?> StatusAsync();
    
    // PlayAsync — generic primitive mirroring the server convention api/role/{rolename}/{scenario}.
    // GET overload: uid is appended as the sole query param only when non-empty (director scenarios
    // pass none). POST overload: the body is serialized as JSON. PlayAllowingClientErrorAsync is the
    // POST variant that recovers a structured 4xx body instead of throwing (WR-02).
    Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, string? uid = null, CancellationToken ct = default);
    Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default);
    Task<ApiEnvelope<T>?> PlayAllowingClientErrorAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default);
    
    
}
