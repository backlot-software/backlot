using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // 1. Base64-encode "username:password" (stored WITHOUT "Basic " prefix)
        var raw = $"{Input.Username}:{Input.Password}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));

        // 2. Temporarily store so BasicAuthHandler can inject it on the validation call
        HttpContext.Session.SetString("BasicAuthHeader", encoded);
        bool isValid;
        try
        {
            isValid = await _apiClient.IsAuthenticatedAsync();
        }
        catch
        {
            HttpContext.Session.Remove("BasicAuthHeader");
            ModelState.AddModelError(string.Empty, "Could not reach the Backlot API. Try again.");
            return Page();
        }

        if (!isValid)
        {
            // Remove invalid credentials from session
            HttpContext.Session.Remove("BasicAuthHeader");
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        // 3. Build ClaimsPrincipal and sign in cookie auth (AFTER API confirms credentials)
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, Input.Username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return LocalRedirect(ReturnUrl ?? "/");
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
