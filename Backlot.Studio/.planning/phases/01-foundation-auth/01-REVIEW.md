---
phase: 01-foundation-auth
reviewed: 2026-06-22T00:00:00Z
depth: standard
files_reviewed: 21
files_reviewed_list:
  - Backlot.Studio.csproj
  - Program.cs
  - appsettings.json
  - Pages/Shared/_Layout.cshtml
  - Pages/Shared/_LoginLayout.cshtml
  - Pages/Shared/_Sidebar.cshtml
  - Pages/_ViewImports.cshtml
  - Pages/_ViewStart.cshtml
  - wwwroot/css/studio.css
  - wwwroot/js/studio.js
  - Services/ApiEnvelope.cs
  - Services/BacklotApiUnauthorizedException.cs
  - Services/BasicAuthHandler.cs
  - Services/IBacklotApiClient.cs
  - Services/BacklotApiClient.cs
  - Pages/AuthenticatedPageModel.cs
  - Pages/Login.cshtml
  - Pages/Login.cshtml.cs
  - Pages/Logout.cshtml
  - Pages/Logout.cshtml.cs
  - Pages/Index.cshtml
  - Pages/Index.cshtml.cs
findings:
  critical: 2
  warning: 3
  info: 2
  total: 7
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-06-22
**Depth:** standard
**Files Reviewed:** 21
**Status:** issues_found

## Summary

This phase delivers the foundation: project scaffold, auth plumbing (cookie auth + session-stored API credentials + delegating handler), a login/logout flow, and a protected dashboard page. The architecture choices are sound — the split between the ASP.NET Core cookie (for gating) and the session-stored Basic auth header (for proxying to the Backlot API) is clean and correctly implemented at the component level.

Two blockers exist. The most critical is a silent redirect failure in `AuthenticatedPageModel.SafeApiCall`: it calls `Response.Redirect()` and returns `default(T?)`, but the caller (`IndexModel.OnGetAsync`) continues executing and returns `Page()`, which overwrites the 302 redirect with a 200 and renders the page body instead. The redirect never fires. The second blocker is a credential leak: credentials written to session before API validation are not cleaned up if the validation call throws an unexpected exception (network down, API 500), leaving stale credentials permanently attached to the session.

Three warnings cover: missing `UseSession()` before `UseAuthentication()` (session is unavailable if the pipeline short-circuits at auth), leaked credentials on network error, and the scaffolded `Privacy` page being publicly accessible without authentication.

---

## Critical Issues

### CR-01: `SafeApiCall` redirect is silently swallowed — `IndexModel` always renders `Page()`

**File:** `Pages/AuthenticatedPageModel.cs:18`, `Pages/Index.cshtml.cs:36`

**Issue:** `SafeApiCall` handles `BacklotApiUnauthorizedException` by calling `Response.Redirect("/login")` (which sets a 302 status on the response) and then returns `default(T?)`. The caller, `IndexModel.OnGetAsync`, receives `null`, continues the normal code path, and hits `return Page()`. Razor Pages then executes the `PageResult`, which sets `StatusCode = 200` and renders the page body, overwriting the 302. The redirect **never fires**; the user sees the dashboard with `"Unknown user"` instead of being sent to the login page.

**Fix:** Change `SafeApiCall` to return an `IActionResult?` redirect signal that the caller must propagate, or restructure to use a re-thrown sentinel exception. The cleanest approach for Razor Pages:

```csharp
// AuthenticatedPageModel.cs
protected async Task<(T? Value, IActionResult? Redirect)> SafeApiCall<T>(Func<Task<T>> apiCall)
{
    try
    {
        return (await apiCall(), null);
    }
    catch (Services.BacklotApiUnauthorizedException)
    {
        Response.Headers["Turbo-Visit-Control"] = "reload";
        return (default, RedirectToPage("/Login"));
    }
}

// IndexModel.OnGetAsync
var (result, redirect) = await SafeApiCall(async () => await _api.WhoAmIAsync());
if (redirect != null) return redirect;
```

---

### CR-02: Credentials persist in session when `IsAuthenticatedAsync` throws an unexpected exception

**File:** `Pages/Login.cshtml.cs:40-41`

**Issue:** `OnPostAsync` writes the Base64-encoded credentials to session (`SetString("BasicAuthHeader", encoded)`) before calling `IsAuthenticatedAsync()`. There is no `try/catch` around the API call. If `IsAuthenticatedAsync` throws anything other than `BacklotApiUnauthorizedException` — for example `HttpRequestException` (API unreachable, network timeout, non-2xx status code from `EnsureSuccessStatusCode`) — the exception propagates as an unhandled 500, but the session still contains the credentials. On any subsequent request in that session (including the error page), `BasicAuthHandler` will inject those credentials into API calls even though the login was never successfully validated.

```csharp
// Login.cshtml.cs OnPostAsync — vulnerable path
HttpContext.Session.SetString("BasicAuthHeader", encoded);   // written
var isValid = await _apiClient.IsAuthenticatedAsync();       // can throw — no try/catch
// if it throws, session credential is never removed
```

**Fix:** Wrap the validation in a try/finally (or try/catch) that always removes the session credential on failure:

```csharp
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
    HttpContext.Session.Remove("BasicAuthHeader");
    ModelState.AddModelError(string.Empty, "Invalid username or password.");
    return Page();
}
```

---

## Warnings

### WR-01: `UseSession()` is registered after `UseAuthentication()` and `UseAuthorization()` in the middleware pipeline

**File:** `Program.cs:50-52`

**Issue:** The pipeline order is `UseAuthentication → UseAuthorization → UseSession → MapRazorPages`. When `UseAuthorization` short-circuits a request (e.g., redirects an unauthenticated request to `/login`), `UseSession` has not yet been entered — meaning no session is committed and no session-state is available during the auth middleware pass. While cookie authentication does not itself read the session, any future middleware or filter inserted between `UseAuthorization` and `UseSession` that touches `HttpContext.Session` will silently get an uninitialized session. The standard documented order is `UseSession` before `UseAuthentication`.

**Fix:** Move `UseSession()` above `UseAuthentication()`:

```csharp
app.UseRouting();
app.UseSession();          // must come before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

---

### WR-02: Scaffolded `Privacy` page is unauthenticated and reachable by anyone

**File:** `Pages/Privacy.cshtml.cs:6`

**Issue:** `PrivacyModel` has no `[Authorize]` attribute and no global fallback policy is configured (`AddRazorPages()` with no options). Any visitor can navigate to `/Privacy` without credentials. This is harmless in the current template content, but the app has no defense-in-depth against future content being added to that page without an explicit authorization check.

**Fix:** Either add `[Authorize]` to `PrivacyModel`, or configure a global fallback policy in `Program.cs` that requires authentication and mark `/login`, `/logout`, and `/privacy` (if intentionally public) with `[AllowAnonymous]`:

```csharp
// Program.cs — recommended global policy
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

---

### WR-03: Login `OnGet` does not redirect already-authenticated users

**File:** `Pages/Login.cshtml.cs:26-29`

**Issue:** `OnGet()` returns `Page()` unconditionally. An already-authenticated user who navigates to `/login` sees the login form again. If they submit it, `SignInAsync` is called a second time with potentially different credentials, silently replacing the session's `BasicAuthHeader`. This creates a confused state where the auth cookie says "user A" but the session credential is for user B (or an invalid credential if the API rejects the new attempt).

**Fix:** Check authentication state in `OnGet`:

```csharp
public IActionResult OnGet()
{
    if (User.Identity?.IsAuthenticated == true)
        return LocalRedirect(ReturnUrl ?? "/");
    return Page();
}
```

---

## Info

### IN-01: `_Sidebar.cshtml` uses a hardcoded `action="/logout"` instead of a tag-helper expression

**File:** `Pages/Shared/_Sidebar.cshtml:27`

**Issue:** The logout form uses `action="/logout"` (literal path string). If the app is ever deployed under a path prefix (e.g., behind a reverse proxy at `/studio/`), this hardcoded path will break. All other forms in the codebase use `asp-page`.

**Fix:**
```cshtml
<form method="post" asp-page="/Logout" class="mt-1">
```

---

### IN-02: `BacklotApiClient.GetEnvelopeAsync` calls `EnsureSuccessStatusCode()` but non-2xx/non-401 errors (e.g., 403, 500) surface as untyped `HttpRequestException`

**File:** `Services/BacklotApiClient.cs:17`

**Issue:** For any HTTP error other than 401 (which `BasicAuthHandler` converts to `BacklotApiUnauthorizedException`), `EnsureSuccessStatusCode()` throws `HttpRequestException`. Callers like `IndexModel` and `Login.cshtml.cs` do not handle this type — leading to unhandled 500 responses surfaced directly to the browser. This is not a blocking issue for phase 1 (WhoAmI and IsAuthenticated rarely return 403/500) but will become a gap as more endpoints are added in phase 2.

**Fix:** Add a `catch (HttpRequestException)` layer in `BacklotApiClient` or in `SafeApiCall`, or define a specific `BacklotApiException` wrapper that carries the status code for informed error display.

---

_Reviewed: 2026-06-22_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
