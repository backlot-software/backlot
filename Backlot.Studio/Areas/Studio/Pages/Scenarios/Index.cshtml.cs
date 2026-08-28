using System.Text;
using Backlot.Studio.Core;
using Backlot.Studio.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Areas.Studio.Pages.Scenarios;

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
            var (result, redirect) = await SafeApiCall(async () => await _api.Play<IEnumerable<ScenarioItem>>("scenarios"));
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

    // Downloads the TypeSpec contract for the endpoints this user may play, for a consumer to run
    // through `tsp compile`. A named handler rather than part of the page load: it is a build-time
    // artifact, and nothing on the scenario list depends on it.
    public async Task<IActionResult> OnGetSpecAsync()
    {
        SetUserContext();

        var (result, redirect) = await SafeApiCall(async () => await _api.Play<string>("scenariospec"));
        if (redirect != null) return redirect;

        var spec = result?.Body;

        if (string.IsNullOrWhiteSpace(spec))
        {
            // Same degrade-rather-than-fail stance as the examples below: send the operator back to
            // a working page instead of an error one.
            _logger.LogWarning("Backlot API returned an empty TypeSpec contract");
            return RedirectToPage();
        }

        return File(Encoding.UTF8.GetBytes(spec), "text/plain; charset=utf-8", "backlot.tsp");
    }

    // Separate from the scenario list on purpose: the examples are reflected server-side and cost
    // more to produce, so a failure here degrades the page to a plain list instead of an error.
    private async Task<Dictionary<string, ScenarioSchemaItem>> LoadExamples()
    {
        try
        {
            var envelope = await _api.Play<IEnumerable<ScenarioSchemaItem>>("scenarioschemas");
            return (envelope?.Body ?? [])
                .GroupBy(e => e.Scenario, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to load scenario examples from Backlot API");
            return new Dictionary<string, ScenarioSchemaItem>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
