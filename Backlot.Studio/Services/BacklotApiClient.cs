using System.Net.Http.Json;
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
}
