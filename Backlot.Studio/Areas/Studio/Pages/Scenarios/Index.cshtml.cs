using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Scenarios;

public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public List<(string Category, IEnumerable<ScenarioItem> Scenarios)> Groups { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    // Request/response examples keyed by scenario name. Best-effort: the page renders without them
    // if the API does not serve them, so an older API never breaks the scenario list.
    public Dictionary<string, ScenarioSchemaItem> Examples { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();
        try
        {
            var (result, redirect) = await SafeApiCall(async () => await _api.Get<IEnumerable<ScenarioItem>>("director", "scenarios"));
            if (redirect != null) return redirect;
            Groups = (result?.Body ?? [])
                .GroupBy(s => s.Tags.Length > 0 ? s.Tags[0] : "Uncategorized")
                .Select(g => (g.Key, g.AsEnumerable()))
                .ToList();

            Examples = await LoadExamples();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load scenarios from Backlot API");
            ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable and that your credentials are valid.";
        }
        return Page();
    }

    // Separate from the scenario list on purpose: the examples are reflected server-side and cost
    // more to produce, so a failure here degrades the page to a plain list instead of an error.
    private async Task<Dictionary<string, ScenarioSchemaItem>> LoadExamples()
    {
        try
        {
            var envelope = await _api.Get<IEnumerable<ScenarioSchemaItem>>("director", "scenarioschemas");
            return (envelope?.Body ?? [])
                .GroupBy(e => e.Scenario, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is not BacklotApiUnauthorizedException)
        {
            _logger.LogWarning(ex, "Failed to load scenario examples from Backlot API");
            return new Dictionary<string, ScenarioSchemaItem>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
