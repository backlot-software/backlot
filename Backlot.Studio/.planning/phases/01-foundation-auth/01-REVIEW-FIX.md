---
phase: 01-foundation-auth
fixed_at: 2026-06-22T00:00:00Z
review_path: .planning/phases/01-foundation-auth/01-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 5
skipped: 0
status: all_fixed
---

# Phase 01: Code Review Fix Report

**Fixed at:** 2026-06-22
**Source review:** .planning/phases/01-foundation-auth/01-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 5 (2 Critical, 3 Warning)
- Fixed: 5
- Skipped: 0

## Fixed Issues

### CR-01: `SafeApiCall` redirect is silently swallowed

**Files modified:** `Backlot.Studio/Pages/AuthenticatedPageModel.cs`, `Backlot.Studio/Pages/Index.cshtml.cs`
**Commit:** a2f20af
**Applied fix:** Changed `SafeApiCall<T>` return type from `T?` to `(T? Value, IActionResult? Redirect)`. On `BacklotApiUnauthorizedException`, returns `(default, RedirectToPage("/Login"))` instead of calling `Response.Redirect()` and returning `default`. Updated `IndexModel.OnGetAsync` to destructure the tuple and propagate the redirect via `if (redirect != null) return redirect;` before continuing page rendering.

---

### CR-02: Credentials persist in session when `IsAuthenticatedAsync` throws an unexpected exception

**Files modified:** `Backlot.Studio/Pages/Login.cshtml.cs`
**Commit:** 22c9bc8
**Applied fix:** Wrapped the `_apiClient.IsAuthenticatedAsync()` call in a `try/catch` block. The catch handler calls `HttpContext.Session.Remove("BasicAuthHeader")`, adds a user-facing model error ("Could not reach the Backlot API. Try again."), and returns `Page()`. This guarantees credentials are never left in session if the API call throws (network error, timeout, non-2xx, etc.).

---

### WR-01: `UseSession()` registered after `UseAuthentication()` and `UseAuthorization()`

**Files modified:** `Backlot.Studio/Program.cs`
**Commit:** fd8f3e1
**Applied fix:** Moved `app.UseSession()` above `app.UseAuthentication()` in the middleware pipeline. Updated the inline comment to clarify the ordering requirement.

---

### WR-02: Scaffolded `Privacy` page is unauthenticated (no Privacy page found — global policy applied)

**Files modified:** `Backlot.Studio/Program.cs`, `Backlot.Studio/Pages/Login.cshtml.cs`
**Commit:** 3bd19af
**Applied fix:** The Privacy page does not exist in this codebase (reviewer anticipated it from the default scaffold). Applied the recommended global fallback policy instead: added `builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` to `Program.cs`. Added the `using Microsoft.AspNetCore.Authorization;` import. Added `[AllowAnonymous]` to `LoginModel` (and its `using` import) so the login page remains accessible to unauthenticated users with the global policy in effect. `LogoutModel` already had `[Authorize]` — no change needed.

---

### WR-03: Login `OnGet` does not redirect already-authenticated users

**Files modified:** `Backlot.Studio/Pages/Login.cshtml.cs`
**Commit:** 7701b21
**Applied fix:** Added an authentication check at the top of `OnGet()`: `if (User.Identity?.IsAuthenticated == true) return LocalRedirect(ReturnUrl ?? "/");`. Authenticated users are now redirected away from the login form immediately, preventing credential confusion from re-submission.

---

_Fixed: 2026-06-22_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
