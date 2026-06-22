using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages;

[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;

    public IndexModel(IBacklotApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var (_, redirect) = await SafeApiCall(async () => await _api.WhoAmIAsync());
        if (redirect != null) return redirect;

        SetUserContext();
        ViewData["ActiveNav"] = "";

        return Page();
    }
}
