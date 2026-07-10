using System.Net.Http.Json;
using System.Text.Json;
using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public class BacklotApiClient : IBacklotApiClient
{
    public Uri BaseUrl { get; private set; }
    
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

        if (httpClient.BaseAddress == null)
            throw new ArgumentException($"BaseAddress is required for {nameof(BacklotApiClient)}");
        
        BaseUrl = httpClient.BaseAddress;
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
        var envelope = await PlayAsync<bool>("director", "isauthenticated");
        return envelope?.Body ?? false;
    }

    // WhoAmIAsync — called server-side from authenticated PageModels
    public async Task<object?> WhoAmIAsync()
    {
        var envelope = await PlayAsync<object>("director", "whoami");
        return envelope?.Body;
    }
    
    public async Task<ApiEnvelope<StatusBody>?> StatusAsync()
    {

        var path = $"api/status";
        var response = await _httpClient.GetAsync(path);
        await EnsureSuccessAsync(response, CancellationToken.None);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<StatusBody>>(JsonOptions, CancellationToken.None);
    }

    // SendRawAsync — send an arbitrary (method + path + body) request through the authenticated
    // pipeline for the Client tester page. Deliberately does NOT call EnsureSuccessAsync: the raw
    // status, reason and body are returned for every non-401 outcome so the operator can inspect
    // the response regardless of success. (401 still throws BacklotApiUnauthorizedException from
    // BasicAuthHandler, which the caller translates into a re-login.) The path is used relative to
    // BaseAddress; a leading slash is tolerated. A JSON body is attached only for methods that
    // carry one.
    public async Task<RawApiResponse> SendRawAsync(string method, string path, string? body, CancellationToken ct = default)
    {
        var httpMethod = new HttpMethod((method ?? "GET").Trim().ToUpperInvariant());
        using var request = new HttpRequestMessage(httpMethod, path.Trim());

        var carriesBody = httpMethod == HttpMethod.Post
                          || httpMethod == HttpMethod.Put
                          || httpMethod == HttpMethod.Patch
                          || httpMethod == HttpMethod.Delete;
        
        if (carriesBody && !string.IsNullOrWhiteSpace(body))
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.SendAsync(request, ct);
        stopwatch.Stop();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        return new RawApiResponse
        {
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? response.StatusCode.ToString(),
            Body = responseBody,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            IsSuccess = response.IsSuccessStatusCode
        };
    }
}
