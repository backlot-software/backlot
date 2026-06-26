using System.Net.Http.Json;
using System.Text.Json;
using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public class BacklotApiClient : IBacklotApiClient
{
    private readonly HttpClient _httpClient;

    // One shared options instance used for BOTH serialization and deserialization so casing
    // behaviour is intentional and consistent (WR-04). Backlot envelopes and nested DTOs
    // (ValidationOutcome.Results[].ErrorMessage/MemberNames) are PascalCase; General defaults
    // use PascalCase property naming. PropertyNameCaseInsensitive stays on as a tolerant
    // fallback while the live PascalCase shape is being confirmed.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNameCaseInsensitive = true
    };

    public BacklotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Throw a rich BacklotApiException (status + body) on non-success instead of the bare
    // EnsureSuccessStatusCode HttpRequestException, which discards the API's diagnostic body
    // (WR-05). The body is read before throwing so callers/logs retain the detail.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new BacklotApiException(response.StatusCode, body);
    }

    private async Task<ApiEnvelope<T>?> GetEnvelopeAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
    }

    private async Task<ApiEnvelope<T>?> PostEnvelopeAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
    }

    // PlayAsync (GET) — generic primitive for the api/role/{rolename}/{scenario} convention.
    // The uid is appended as the sole query param only when non-empty (mirroring the GET branch in
    // ApplicationBuilding.cs: director scenarios pass no uid; other roles require uid as the only
    // query param). uid is escaped via Uri.EscapeDataString to prevent query/path injection (T-ou7-01).
    public async Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, string? uid = null, CancellationToken ct = default)
    {
        var path = $"api/role/{roleName}/{scenario}";
        if (!string.IsNullOrEmpty(uid))
            path += $"?uid={Uri.EscapeDataString(uid)}";

        var response = await _httpClient.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
    }

    // PlayAsync (POST) — generic primitive that posts the body as JSON to api/role/{rolename}/{scenario}.
    public async Task<ApiEnvelope<T>?> PlayAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default)
    {
        var path = $"api/role/{roleName}/{scenario}";
        var response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
    }

    // PlayAllowingClientErrorAsync (POST) — like PlayAsync(POST) but recovers a structured outcome
    // from a 4xx error body instead of throwing. The API may signal validation failure with a non-2xx
    // status while still returning a structured body; read and deserialize that body for client (4xx,
    // excluding 401/403) responses so per-field results survive (WR-02). Auth (401/403) and 5xx
    // responses still throw via EnsureSuccessAsync so the caller surfaces them. If the error body
    // isn't a recognizable envelope, fall back to throwing the rich exception so the failure isn't
    // silently swallowed.
    public async Task<ApiEnvelope<T>?> PlayAllowingClientErrorAsync<T>(string roleName, string scenario, object body, CancellationToken ct = default)
    {
        var path = $"api/role/{roleName}/{scenario}";
        var response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);

        var isClientValidationFailure =
            (int)response.StatusCode is >= 400 and < 500
            && response.StatusCode != System.Net.HttpStatusCode.Unauthorized
            && response.StatusCode != System.Net.HttpStatusCode.Forbidden;

        if (!response.IsSuccessStatusCode && !isClientValidationFailure)
        {
            await EnsureSuccessAsync(response, ct);
        }

        if (isClientValidationFailure)
        {
            try
            {
                var failEnvelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
                if (failEnvelope is { Body: { } })
                    return failEnvelope;
            }
            catch (JsonException)
            {
                // fall through to throw with the raw body
            }
            await EnsureSuccessAsync(response, ct);
        }

        return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, ct);
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

    // GetRoleDetailAsync — fetches full dynamic role detail by UID via seekbase/detail.
    // The returned element is the UNWRAPPED role: Body.Role when the seekbase/detail wrapper
    // (`{ "Role": {…flattened role…}, "Relations": [...] }`) is present, else Body itself.
    // Unwrapping at this single chokepoint means every consumer
    // (DetailModel.GetPermissions/GetSkills/GetNonSystemFields/GetPageTitle and the Edit-page
    // server-side CanWrite gate + field seeding) reads __Permission/__Skills/__LastModifiedDate
    // and data fields at the correct level without any PageModel change.
    public async Task<JsonElement?> GetRoleDetailAsync(string uid, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<JsonElement>("api/role/seekbase/detail", new { For = uid }, ct);
        if (envelope is null) return null;
        return UnwrapRoleDetail(envelope.Body);
    }

    // UnwrapRoleDetail — defensively descends into the seekbase/detail `Role` wrapper.
    // Only descends when the Body is an object that actually contains a `Role` object, so a
    // future flat response shape (role fields already at the top level, no Role wrapper) and any
    // non-object Body pass through unchanged. role.Clone() detaches the sub-tree so the returned
    // JsonElement stays valid after the parent JsonDocument (from ReadFromJsonAsync) is disposed.
    private static JsonElement UnwrapRoleDetail(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return body;

        if (body.TryGetProperty("Role", out var role) && role.ValueKind == JsonValueKind.Object)
            return role.Clone();

        return body;
    }

    // GetRoleRelationsAsync — fetches related roles for a given UID via persist/relations
    public async Task<IEnumerable<RelationItem>?> GetRoleRelationsAsync(string uid, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<IEnumerable<RelationItem>>("api/role/persist/relations", new { Uid = uid }, ct);
        return envelope?.Body;
    }

    // GetRoleSchemaAsync — fetches all role-type field schemas via director/roles
    public async Task<IReadOnlyList<RoleSchema>?> GetRoleSchemaAsync(CancellationToken ct = default)
    {
        var envelope = await GetEnvelopeAsync<IReadOnlyList<RoleSchema>>("api/role/director/roles", ct);
        return envelope?.Body;
    }

    // ValidateRoleAsync — server-side validation via role/isvalid (does not persist).
    // The API may signal validation failure with a non-2xx status while still returning a
    // structured ValidationOutcome body. Read and deserialize that body for 4xx responses
    // rather than throwing on EnsureSuccessStatusCode, so per-field validation results survive
    // and reach the 422 form path instead of collapsing into the generic "Save failed" banner
    // (WR-02). Auth (401/403) and 5xx responses still throw so the caller surfaces them.
    public async Task<ValidationOutcome?> ValidateRoleAsync(object roleData, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/role/role/isvalid", roleData, JsonOptions, ct);

        var isClientValidationFailure =
            (int)response.StatusCode is >= 400 and < 500
            && response.StatusCode != System.Net.HttpStatusCode.Unauthorized
            && response.StatusCode != System.Net.HttpStatusCode.Forbidden;

        if (!response.IsSuccessStatusCode && !isClientValidationFailure)
        {
            await EnsureSuccessAsync(response, ct);
        }

        if (isClientValidationFailure)
        {
            // Try to recover the structured outcome from the error body. If the body isn't a
            // recognizable envelope, fall back to throwing the rich exception so the failure
            // isn't silently swallowed.
            try
            {
                var failEnvelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ValidationOutcome>>(JsonOptions, ct);
                if (failEnvelope?.Body is { } body)
                    return body;
            }
            catch (JsonException)
            {
                // fall through to throw with the raw body
            }
            await EnsureSuccessAsync(response, ct);
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ValidationOutcome>>(JsonOptions, ct);
        return envelope?.Body;
    }

    // PersistRoleAsync — saves/updates a role via persist/persist
    public async Task<JsonElement?> PersistRoleAsync(object roleData, CancellationToken ct = default)
    {
        var envelope = await PostEnvelopeAsync<JsonElement>("api/role/persist/persist", roleData, ct);
        return envelope?.Body;
    }
}
