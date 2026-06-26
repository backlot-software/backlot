using System.Text.Json;
using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public interface IBacklotApiClient
{
    Task<bool> IsAuthenticatedAsync();
    Task<object?> WhoAmIAsync();
    Task<IEnumerable<ScenarioItem>?> GetScenariosAsync();
    Task<FindResult?> FindRolesAsync(FindRequest request, CancellationToken ct = default);
    Task<JsonElement?> GetRoleDetailAsync(string uid, CancellationToken ct = default);
    Task<IEnumerable<RelationItem>?> GetRoleRelationsAsync(string uid, CancellationToken ct = default);
    Task<IReadOnlyList<RoleSchema>?> GetRoleSchemaAsync(CancellationToken ct = default);
    Task<ValidationOutcome?> ValidateRoleAsync(object roleData, CancellationToken ct = default);
    Task<JsonElement?> PersistRoleAsync(object roleData, CancellationToken ct = default);

    // PlayAsync — generic primitive mirroring the server convention api/role/{rolename}/{scenario}.
    // GET overload: uid is appended as the sole query param only when non-empty (director scenarios
    // pass none). POST overload: the body is serialized as JSON. PlayAllowingClientErrorAsync is the
    // POST variant that recovers a structured 4xx body instead of throwing (WR-02).
    Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, string? uid = null, CancellationToken ct = default);
    Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default);
    Task<ApiEnvelope<T>?> PlayAllowingClientErrorAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default);
}
