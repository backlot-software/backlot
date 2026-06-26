using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Scenarios;

[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public List<(string Category, IEnumerable<ScenarioItem> Scenarios)> Groups { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

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
            var (result, redirect) = await SafeApiCall(async () => await _api.PlayAsync<IEnumerable<ScenarioItem>>("director", "scenarios"));
            if (redirect != null) return redirect;
            Groups = (result?.Body ?? [])
                .GroupBy(s => s.Tags.Length > 0 ? s.Tags[0] : "Uncategorized")
                .Select(g => (g.Key, g.AsEnumerable()))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load scenarios from Backlot API");
            ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable and that your credentials are valid.";
        }
        return Page();
    }
}
