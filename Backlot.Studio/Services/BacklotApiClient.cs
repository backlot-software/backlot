using System.Net.Http.Json;
using System.Text.Json;
using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public class BacklotApiClient : IBacklotApiClient
{
    private readonly HttpClient _httpClient;

    public BacklotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<ApiEnvelope<T>?> GetEnvelopeAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
    }

    private async Task<ApiEnvelope<T>?> PostEnvelopeAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(path, body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
    }

    // IsAuthenticatedAsync — called from Login.cshtml.cs to validate credentials
    public async Task<bool> IsAuthenticatedAsync()
    {
        var envelope = await GetEnvelopeAsync<bool>("api/role/director/isauthenticated");
        return envelope?.Body ?? false;
    }

    // WhoAmIAsync — called server-side from authenticated PageModels
    public async Task<object?> WhoAmIAsync()
    {
        var envelope = await GetEnvelopeAsync<object>("api/role/director/whoami");
        return envelope?.Body;
    }

    // GetScenariosAsync — fetches all registered scenarios from the Backlot API
    public async Task<IEnumerable<ScenarioItem>?> GetScenariosAsync()
    {
        var envelope = await GetEnvelopeAsync<IEnumerable<ScenarioItem>>("api/role/director/scenarios");
        return envelope?.Body;
    }

    // FindRolesAsync — searches/paginates roles via simplequery/find
    public async Task<FindResult?> FindRolesAsync(FindRequest request, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<FindResult>("api/role/simplequery/find", request, ct);
        return envelope?.Body;
    }

    // GetRoleDetailAsync — fetches full dynamic role detail by UID via seekbase/detail
    public async Task<JsonElement?> GetRoleDetailAsync(string uid, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<JsonElement>("api/role/seekbase/detail", new { For = uid }, ct);
        return envelope?.Body;
    }

    // GetRoleRelationsAsync — fetches related roles for a given UID via persist/relations
    public async Task<IEnumerable<RelationItem>?> GetRoleRelationsAsync(string uid, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<IEnumerable<RelationItem>>("api/role/persist/relations", new { Uid = uid }, ct);
        return envelope?.Body;
    }
}
