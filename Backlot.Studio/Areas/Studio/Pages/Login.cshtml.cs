using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Backlot.Studio.Services;

namespace Backlot.Studio.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IBacklotApiClient _apiClient;

    public LoginModel(IBacklotApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    [FromQuery]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Authenticate explicitly: the Studio runs on its own scheme, which is never the host's
        // default, so HttpContext.User is not populated on this anonymous page.
        var existing = await HttpContext.AuthenticateAsync(BacklotStudioDefaults.AuthenticationScheme);
        if (existing.Succeeded)
            return LocalRedirect(ReturnUrl ?? DashboardUrl);
        return Page();
    }

    private string DashboardUrl => Url.Page("/Index") ?? "/";

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // 1. Base64-encode "username:password" (stored WITHOUT "Basic " prefix)
        var raw = $"{Input.Username}:{Input.Password}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));

        // 2. Temporarily store so BasicAuthHandler can inject it on the validation call
        HttpContext.Session.SetString(BacklotStudioDefaults.BasicAuthSessionKey, encoded);
        bool isValid;
        try
        {
            isValid = await _apiClient.IsAuthenticated();
        }
        catch
        {
            HttpContext.Session.Remove(BacklotStudioDefaults.BasicAuthSessionKey);
            ModelState.AddModelError(string.Empty, "Could not reach the Backlot API. Try again.");
            return Page();
        }

        if (!isValid)
        {
            // Remove invalid credentials from session
            HttpContext.Session.Remove(BacklotStudioDefaults.BasicAuthSessionKey);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        // 3. Build ClaimsPrincipal and sign in cookie auth (AFTER API confirms credentials)
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, Input.Username) };
        var identity = new ClaimsIdentity(claims, BacklotStudioDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            BacklotStudioDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return LocalRedirect(ReturnUrl ?? DashboardUrl);
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
