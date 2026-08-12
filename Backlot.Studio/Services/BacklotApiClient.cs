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
    public async Task<ApiEnvelope<TR>> Get<TR>(string roleName, string scenario, CancellationToken ct = default)
    {
        var path = $"api/role/{roleName}/{scenario}";

        var response = await _httpClient.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        // todo: throw exception when ReadFromJson returns null.
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TR>>(JsonOptions, ct) ?? throw new InvalidOperationException();
    }

    public async Task<ApiEnvelope<TR>> Get<TR>(string roleName, string scenario, string uid, CancellationToken ct = default)
    {
        var path = $"api/role/{roleName}/{scenario}";
        
        if (string.IsNullOrEmpty(uid)) throw new ArgumentException("Uid is required");
        
        path += $"?uid={Uri.EscapeDataString(uid)}";


        var response = await _httpClient.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TR>>(JsonOptions, ct) ?? throw new InvalidOperationException();
    }

    // PlayAsync (POST) — generic primitive that posts the body as JSON to api/role/{rolename}/{scenario}.
    public async Task<ApiEnvelope<TR>> Post<TB,TR>(string roleName, string scenario, TB body, CancellationToken ct = default) where TB: IRequestBody
    {
        var path = $"api/role/{roleName}/{scenario}";
        var response = await _httpClient.PostAsJsonAsync(path, body, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ApiEnvelope<TR>>(JsonOptions, ct) ?? throw new InvalidOperationException();
    }

    // IsAuthenticatedAsync — called from Login.cshtml.cs to validate credentials
    public async Task<bool> IsAuthenticated()
    {
        var envelope = await Get<bool>("director", "isauthenticated");
        return envelope?.Body ?? false;
    }

    // WhoAmIAsync — called server-side from authenticated PageModels
    public async Task<object> WhoAmI()
    {
        var envelope = await Get<object>("director", "whoami");
        return envelope.Body;
    }
    
    public async Task<ApiEnvelope<StatusBody>?> Status()
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
        var httpMethod = new HttpMethod(method.Trim().ToUpperInvariant());
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = httpMethod == HttpMethod.Post
            ? await _httpClient.PostAsync(path, new StringContent(body ?? string.Empty, System.Text.Encoding.UTF8, "application/json"), ct)
            : await _httpClient.GetAsync(path, ct);
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
