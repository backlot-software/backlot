using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages;

[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    public StatusBody? Status { get; set; } 
    
    private readonly IBacklotApiClient _api;

    public IndexModel(IBacklotApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var (_, redirect) = await SafeApiCall(async () => await _api.WhoAmIAsync());
        if (redirect != null) return redirect; // first check authentication.
        
        var (status, statusRedirect) = await SafeApiCall(async () => await _api.StatusAsync());
        if (statusRedirect != null) return statusRedirect;
        
        Status = status?.Body;
        
        SetUserContext();

        return Page();
    }
}
