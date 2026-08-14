using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Roles;

public class RelationsModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<RelationsModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string Uid { get; set; }

    public IEnumerable<RelationItem> Relations { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public RelationsModel(IBacklotApiClient api, ILogger<RelationsModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();

        if (string.IsNullOrWhiteSpace(Uid))
            return Page();

        try
        {
            var (result, redirect) = await SafeApiCall(async () => await _api.Get<IEnumerable<RelationItem>>("persist", "relations", Uid));
            if (redirect != null) return redirect;
            Relations = result?.Body ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load relations for uid={Uid}", Uid);
            ErrorMessage = "Could not load related roles.";
        }

        return Page();
    }
}
