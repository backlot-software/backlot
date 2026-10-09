using Backlot.Studio.Core;
using Backlot.Studio.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Areas.Studio.Pages;

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
        var (_, redirect) = await SafeApiCall(async () => await _api.WhoAmI());
        if (redirect != null) return redirect; // first check authentication.
        
        var (status, statusRedirect) = await SafeApiCall(async () => await _api.Status());
        if (statusRedirect != null) return statusRedirect;
        
        Status = status?.Body;
        
        SetUserContext();

        return Page();
    }
}
