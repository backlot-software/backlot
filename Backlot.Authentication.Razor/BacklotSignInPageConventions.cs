using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Backlot.Authentication.Razor;

/// <summary>
/// Puts the host's own Razor Pages behind the Backlot sign-in, e.g. in <c>AddRazorPages</c>:
/// <code>
/// builder.Services.AddRazorPages(o => o.Conventions.RequireBacklotSignIn("/Portal"));
/// </code>
/// </summary>
public static class BacklotSignInPageConventions
{
    /// <summary>Requires a Backlot sign-in for every page in <paramref name="folderPath"/>.</summary>
    public static PageConventionCollection RequireBacklotSignIn(
        this PageConventionCollection conventions, string folderPath = "/")
        => conventions.AuthorizeFolder(folderPath, BacklotSignInDefaults.AuthorizationPolicy);

    /// <summary>Requires a Backlot sign-in for every page in <paramref name="folderPath"/> of an area.</summary>
    public static PageConventionCollection RequireBacklotSignInForArea(
        this PageConventionCollection conventions, string areaName, string folderPath = "/")
        => conventions.AuthorizeAreaFolder(areaName, folderPath, BacklotSignInDefaults.AuthorizationPolicy);
}
