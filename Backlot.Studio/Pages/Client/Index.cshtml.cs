using System.Text;
using System.Text.Json;
using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Backlot.Studio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Client;

// Client — a lightweight HTTP request tester. The operator picks a method (GET/POST), selects a
// registered scenario from a searchable dropdown (which loads that scenario's endpoint), optionally
// edits the request body, and executes the call. The request is proxied through the authenticated
// Backlot API pipeline so credentials never reach the browser (same boundary as every other page).
[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public string ApiBaseUrl { get; }

    // Flat list of scenarios (with their endpoints) used to populate the searchable dropdown.
    public List<ScenarioItem> Scenarios { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    // The dropdown entries. In normal mode there is one per scenario (its first endpoint). In
    // "from Detail" mode (arrived via Play) the list is filtered to endpoints whose role segment
    // is one of the role's skills, so a scenario may contribute several entries.
    public List<ScenarioSearchOption> Options { get; private set; } = [];

    // Pre-filled request body (the role's persist JSON) when arriving via Play; empty otherwise.
    public string PrefilledBody { get; private set; } = string.Empty;

    // True when the page was opened via the role Detail Play button.
    public bool FromDetail { get; private set; }

    // Endpoint the page should auto-select on load (the scenario chosen on Detail, or the
    // persist/persist option), or null.
    public string? DefaultEndpoint { get; private set; }

    public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
        ApiBaseUrl = api.BaseUrl.AbsoluteUri;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();
        try
        {
            var (result, redirect) = await SafeApiCall(async () =>
                await _api.PlayAsync<IEnumerable<ScenarioItem>>("director", "scenarios"));
            if (redirect != null) return redirect;

            Scenarios = (result?.Body ?? [])
                .Where(s => s.Endpoints.Length > 0)
                .OrderBy(s => s.Scenario, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Consume Play data (session-backed TempData, read once). Present → "from Detail" mode.
            var playBody = TempData["PlayBody"] as string;
            var playSkillsJson = TempData["PlaySkills"] as string;
            var playEndpoint = TempData["PlayEndpoint"] as string;

            if (playBody != null && playSkillsJson != null)
            {
                FromDetail = true;
                PrefilledBody = playBody;

                var skills = new HashSet<string>(
                    JsonSerializer.Deserialize<string[]>(playSkillsJson) ?? [],
                    StringComparer.OrdinalIgnoreCase);

                // One option per (scenario, endpoint) whose role segment is one of the role's skills.
                Options = ScenarioEndpoint.OptionsForSkills(Scenarios, skills);

                // Default to the scenario the operator picked on the Detail page; fall back to
                // persist/persist. Either way it must have survived the skill filter above.
                DefaultEndpoint =
                    Options.FirstOrDefault(o => string.Equals(o.Endpoint, playEndpoint, StringComparison.OrdinalIgnoreCase))?.Endpoint
                    ?? Options.FirstOrDefault(o => o.Endpoint.TrimEnd('/').EndsWith("/persist/persist", StringComparison.OrdinalIgnoreCase))?.Endpoint;
            }
            else
            {
                // Normal mode: one option per scenario (its first endpoint).
                Options = Scenarios
                    .Select(s => new ScenarioSearchOption(s.Scenario, s.Endpoints.First()))
                    .ToList();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load scenarios from Backlot API");
            ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable and that your credentials are valid.";
        }
        return Page();
    }

    public class ExecuteInput
    {
        public string Method { get; set; } = "GET";
        public string Endpoint { get; set; } = string.Empty;
        public string? Body { get; set; }
    }

    [BindProperty]
    public ExecuteInput Input { get; set; } = new();

    // OnPostExecuteAsync — invoked via fetch() from the page. Returns the raw response (status, body,
    // timing) as JSON for the result area. Never lets a non-success API status become an error page;
    // connection failures and expired credentials are reported inline so the operator sees what
    // happened.
    public async Task<IActionResult> OnPostExecuteAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input.Endpoint))
        {
            return new JsonResult(new { error = "An endpoint is required. Select a scenario or type a path." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        try
        {
            var response = await _api.SendRawAsync(Input.Method, Input.Endpoint, Input.Body, ct);
            return new JsonResult(new
            {
                statusCode = response.StatusCode,
                reasonPhrase = response.ReasonPhrase,
                elapsedMs = response.ElapsedMs,
                isSuccess = response.IsSuccess,
                body = response.Body
            });
        }
        catch (BacklotApiUnauthorizedException)
        {
            // Credentials expired/invalid — the session Basic header no longer authenticates. Tell the
            // client to send the operator back through login rather than silently failing.
            return new JsonResult(new { unauthorized = true, error = "Unauthorized — your session may have expired. Please sign in again." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Client request to {Endpoint} failed", Input.Endpoint);
            return new JsonResult(new { error = "Request failed. Check that the Backlot API is reachable." })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }

    // OnPostCopy — builds a ready-to-send raw .http request for the request the operator has
    // composed (the selected method/endpoint plus the current body) and returns it as text for the
    // page to copy to the clipboard. Living here rather than on the role Detail page means the copy
    // works for every scenario endpoint, not just persist.
    public IActionResult OnPostCopy()
    {
        if (string.IsNullOrWhiteSpace(Input.Endpoint))
        {
            return new JsonResult(new { error = "An endpoint is required. Select a scenario or type a path." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        return new JsonResult(new { text = BuildHttpRequest(Input.Method, Input.Endpoint, Input.Body) });
    }

    // Builds the raw .http request text for {method} {baseUrl}/{endpoint} with the given body. The
    // Authorization line carries the same base64 credential the app uses for its own API requests,
    // read from session ("BasicAuthHeader", stored without the "Basic " prefix by Login.cshtml.cs);
    // missing session value → empty. The body block is omitted for GET requests (and empty bodies).
    private string BuildHttpRequest(string method, string endpoint, string? body)
    {
        var baseUrl = _api.BaseUrl.ToString().TrimEnd('/');
        var authHeader = HttpContext?.Session.GetString("BasicAuthHeader") ?? string.Empty;
        var path = endpoint.Trim().TrimStart('/');
        var verb = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();

        var sb = new StringBuilder();
        sb.Append(verb).Append(' ').Append(baseUrl).Append('/').Append(path).Append('\n');
        sb.Append("Content-Type: application/json").Append('\n');
        sb.Append("Authorization: Basic ").Append(authHeader).Append('\n');

        if (verb != "GET" && !string.IsNullOrWhiteSpace(body))
        {
            sb.Append('\n');
            sb.Append(body).Append('\n');
        }

        return sb.ToString();
    }
}
