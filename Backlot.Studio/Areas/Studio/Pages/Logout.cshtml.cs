using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        // Clear session first — destroys the BasicAuthHeader credential (AUTH-02)
        HttpContext.Session.Clear();

        // Invalidate the auth cookie
        await HttpContext.SignOutAsync(BacklotStudioDefaults.AuthenticationScheme);

        return RedirectToPage("/Login");
    }
}
