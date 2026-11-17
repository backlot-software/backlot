using System.ComponentModel.DataAnnotations;
using Backlot.Authentication.Razor.Core;
using Backlot.Authentication.Razor.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Authentication.Razor.Areas.BacklotSignIn.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IBacklotApiClient _apiClient;
    private readonly IBacklotSignInManager _signInManager;
    private readonly BacklotSignInOptions _options;

    public LoginModel(IBacklotApiClient apiClient, IBacklotSignInManager signInManager, BacklotSignInOptions options)
    {
        _apiClient = apiClient;
        _signInManager = signInManager;
        _options = options;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    [FromQuery]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_apiClient.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, BacklotSignInManager.NotConfiguredMessage);
            return Page();
        }

        if (await _signInManager.IsSignedInAsync())
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : _options.EffectiveDefaultReturnUrl);

        // This page is for Basic authentication; a Bearer API signs in with its identity provider.
        try
        {
            if (await _signInManager.GetApiAuthSchemeAsync() == AuthHeaderHandler.BearerScheme)
                return RedirectToPage("/LoginBearer", new { area = BacklotSignInDefaults.AreaName, ReturnUrl });
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Could not reach the Backlot API. Try again.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.SignInBasicAsync(Input.Username, Input.Password, ReturnUrl);
        if (result.Succeeded) return LocalRedirect(result.RedirectUrl!);

        ModelState.AddModelError(string.Empty, result.Error!);
        return Page();
    }

    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
