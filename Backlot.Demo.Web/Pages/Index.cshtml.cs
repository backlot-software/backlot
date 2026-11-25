using System.Text.Json;
using Backlot.Authentication.Razor;
using Backlot.Authentication.Razor.Core;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Demo.Web.Pages;

public class IndexModel(IBacklotApiClient api) : AuthenticatedPageModel
{
    public string? WhoAmI { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();

        var (me, redirect) = await SafeApiCall(() => api.WhoAmI());
        if (redirect is not null) return redirect;

        WhoAmI = JsonSerializer.Serialize(me, new JsonSerializerOptions { WriteIndented = true });
        return Page();
    }
}