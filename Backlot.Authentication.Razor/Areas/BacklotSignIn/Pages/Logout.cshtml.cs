using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Authentication.Razor.Areas.BacklotSignIn.Pages;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly IBacklotSignInManager _signInManager;
    private readonly BacklotSignInOptions _options;

    public LogoutModel(IBacklotSignInManager signInManager, BacklotSignInOptions options)
    {
        _signInManager = signInManager;
        _options = options;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        // Signs out everywhere: the Studio and the host's own pages share the sign-in.
        await _signInManager.SignOutAsync();

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : LocalRedirect(_options.EffectiveLoginPath);
    }
}
