using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
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
}
