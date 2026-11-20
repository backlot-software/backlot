using System.Net.Http.Headers;

namespace Backlot.Authentication.Razor;

/// <summary>
/// Signs users in to the Backlot API and the host, following the scheme the API reports
/// (AuthInfo scenario). Used by the built-in login pages, and by a host that builds its own login
/// page (set <see cref="BacklotSignInOptions.LoginPath"/> to it).
/// </summary>
/// <remarks>
/// The API credentials (Basic credential or Bearer tokens) are kept in the server-side session;
/// only the authentication cookie reaches the browser.
/// </remarks>
public interface IBacklotSignInManager
{
    /// <summary>Whether the current request carries a valid Backlot sign-in cookie.</summary>
    Task<bool> IsSignedInAsync();

    /// <summary>The authentication scheme of the Backlot API: "Basic" or "Bearer".</summary>
    Task<string> GetApiAuthSchemeAsync();

    /// <summary>
    /// Checks the username and password against the Backlot API and, when accepted, signs the user in.
    /// On success <see cref="BacklotSignInResult.RedirectUrl"/> is the validated return url.
    /// </summary>
    Task<BacklotSignInResult> SignInBasicAsync(string username, string password, string? returnUrl);

    /// <summary>
    /// Starts signing in through the identity provider (Backlot.Authentication.Bearer gateway flow).
    /// On success <see cref="BacklotSignInResult.RedirectUrl"/> is the identity provider's login url.
    /// </summary>
    /// <param name="returnUrl">Local url to return to once signed in.</param>
    /// <param name="doneUrl">
    /// Absolute url the API returns the browser to with its code; it must be in the API's
    /// AllowedRedirectUris. The same value has to be passed to <see cref="CompleteBearerAsync"/>.
    /// </param>
    Task<BacklotSignInResult> StartBearerAsync(string? returnUrl, string doneUrl);

    /// <summary>
    /// Exchanges the API's code for tokens and signs the user in.
    /// On success <see cref="BacklotSignInResult.RedirectUrl"/> is the validated return url.
    /// </summary>
    Task<BacklotSignInResult> CompleteBearerAsync(string? code, string? state, string? error, string doneUrl);

    /// <summary>Clears the API credentials and signs the user out of the host.</summary>
    Task SignOutAsync();

    /// <summary>
    /// The Authorization header the signed-in user sends to the Backlot API, or <c>null</c> when
    /// nobody is signed in.
    /// </summary>
    AuthenticationHeaderValue? GetApiAuthorization();
}

/// <summary>Outcome of a sign-in step.</summary>
public sealed record BacklotSignInResult(bool Succeeded, string? RedirectUrl, string? Error)
{
    public static BacklotSignInResult Success(string redirectUrl) => new(true, redirectUrl, null);
    public static BacklotSignInResult Failed(string error) => new(false, null, error);
}
