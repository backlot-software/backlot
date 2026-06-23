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
}
