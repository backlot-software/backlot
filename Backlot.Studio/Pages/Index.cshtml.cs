using System.Text.Json;
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
        var result = await SafeApiCall(async () => await _api.WhoAmIAsync());

        // WhoAmIAsync returns object? — if it is a JsonElement, extract the string value safely (T-03-07)
        string? usernameStr;
        if (result is JsonElement je && je.ValueKind == JsonValueKind.String)
        {
            usernameStr = je.GetString();
        }
        else
        {
            usernameStr = result?.ToString();
        }

        ViewData["Username"] = usernameStr ?? "Unknown user";
        ViewData["ActiveNav"] = "";

        return Page();
    }
}
