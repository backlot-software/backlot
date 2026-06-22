---
phase: 01-foundation-auth
plan: "03"
subsystem: auth-pages
tags: [auth, login, logout, razor-pages, session, cookie-auth, whoami]
status: complete

dependency_graph:
  requires:
    - 01-01 (Backlot.Studio.csproj, _LoginLayout.cshtml, Pages/ structure)
    - 01-02 (IBacklotApiClient, AuthenticatedPageModel, Program.cs DI wiring)
  provides:
    - Backlot.Studio/Pages/Login.cshtml (centered Bootstrap card login page)
    - Backlot.Studio/Pages/Login.cshtml.cs (LoginModel with credential validation and SignInAsync)
    - Backlot.Studio/Pages/Logout.cshtml (minimal redirect-only page)
    - Backlot.Studio/Pages/Logout.cshtml.cs (LogoutModel with Session.Clear and SignOutAsync)
    - Backlot.Studio/Pages/Index.cshtml (authenticated shell root view)
    - Backlot.Studio/Pages/Index.cshtml.cs (IndexModel extends AuthenticatedPageModel, calls WhoAmIAsync)
  affects:
    - AUTH-01, AUTH-02, AUTH-03, AUTH-04 (all requirements now fully satisfied)
    - Plan 02-xx (Index page replaced by Scenarios redirect in Phase 2)

tech_stack:
  added: []
  patterns:
    - Credentials base64-encoded and stored in ISession["BasicAuthHeader"] before API validation
    - API validation via IsAuthenticatedAsync() before HttpContext.SignInAsync (T-03-05)
    - Session.Clear() before SignOutAsync() on logout (AUTH-02)
    - IndexModel extends AuthenticatedPageModel for 401 safety via SafeApiCall
    - JsonElement string extraction for WhoAmIAsync result (T-03-07)
    - data-turbo="false" on login form to prevent Turbo Drive POST interception (AUTH-01)

key_files:
  created:
    - Backlot.Studio/Pages/Login.cshtml
    - Backlot.Studio/Pages/Login.cshtml.cs
    - Backlot.Studio/Pages/Logout.cshtml
    - Backlot.Studio/Pages/Logout.cshtml.cs
    - Backlot.Studio/Pages/Index.cshtml
    - Backlot.Studio/Pages/Index.cshtml.cs
  modified: []

decisions:
  - "LoginModel stores session key before IsAuthenticatedAsync call, removes it on failure — ensures BasicAuthHandler can read credentials during validation without session persisting invalid credentials"
  - "IndexModel uses JsonElement string extraction for WhoAmIAsync result — WhoAmIAsync returns object? which may be a JsonElement at runtime; .GetString() on String ValueKind avoids JSON literal leakage"

metrics:
  duration_minutes: 1
  completed_date: "2026-06-22"
  tasks_completed: 2
  tasks_total: 2
  files_created: 6
  files_modified: 0
---

# Phase 01 Plan 03: Auth Flow Pages Summary

**One-liner:** Login/Logout/Index pages completing the end-to-end auth boundary: credential validation via API before SignIn, session-backed Basic Auth, cookie auth gating, and username display from WhoAmI.

## What Was Built

Created the six auth flow pages that complete Phase 1's AUTH-01 through AUTH-04 requirements.

**Login.cshtml** — centered Bootstrap card using `_LoginLayout` (no sidebar, per D-06). Form has `data-turbo="false"` to prevent Turbo Drive from intercepting the POST. Shows `alert-danger` banner above fields on invalid credentials. Includes `@Html.AntiForgeryToken()` and hidden `ReturnUrl` input for proper post-login redirect.

**Login.cshtml.cs** — `LoginModel : PageModel` (not AuthenticatedPageModel — login is unauthenticated). `OnPostAsync` follows CRITICAL ORDER: base64-encodes credentials → stores in `ISession["BasicAuthHeader"]` → calls `IsAuthenticatedAsync()` → on failure removes session key and adds model error → on success builds `ClaimsPrincipal` with `ClaimTypes.Name` → calls `SignInAsync` with `IsPersistent = false` → `LocalRedirect(ReturnUrl ?? "/")`.

**Logout.cshtml** — minimal page with `Layout = null`. No visible content — only the POST handler matters.

**Logout.cshtml.cs** — `[Authorize] LogoutModel : PageModel`. `OnPostAsync` calls `Session.Clear()` then `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` then `RedirectToPage("/Login")`. The `[Authorize]` attribute prevents anonymous users from triggering logout.

**Index.cshtml.cs** — `[Authorize] IndexModel : AuthenticatedPageModel`. `OnGetAsync` calls `_api.WhoAmIAsync()` wrapped in `SafeApiCall`. Since `WhoAmIAsync` returns `object?` which may be a `JsonElement` at runtime, the code explicitly checks `ValueKind == JsonValueKind.String` and calls `.GetString()` to avoid JSON literal strings in the sidebar. Sets `ViewData["Username"]` and `ViewData["ActiveNav"] = ""`.

**Index.cshtml** — minimal authenticated shell root. Shows "Welcome to Backlot Studio" heading and "Logged in as [username]" paragraph. The sidebar (rendered by `_Layout.cshtml` via `_Sidebar` partial) reads `ViewData["Username"]`.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1: Login page | `d29644b` | feat(01-03): create Login page with credential validation and session storage |
| Task 2: Logout + Index | `6fdcd18` | feat(01-03): create Logout page and authenticated Index shell with whoami |

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None. `ViewData["Username"]` is fully wired: `IndexModel.OnGetAsync` calls `WhoAmIAsync` via `SafeApiCall` and sets the key. The API call will return a value when the Backlot API is running. No hardcoded placeholder flows to the UI.

## Threat Flags

No new threat surface beyond the plan's threat model. All STRIDE mitigations applied as specified:
- T-03-01: `@Html.AntiForgeryToken()` + `asp-page` form tag — APPLIED in Login.cshtml
- T-03-02: Logout is POST-only (`OnPostAsync`) with antiforgery in `_Sidebar.cshtml` — APPLIED
- T-03-03: `data-turbo="false"` on login form, POST method, `autocomplete="current-password"` — APPLIED
- T-03-04: Auth cookie contains only `ClaimTypes.Name` (username), not password — APPLIED
- T-03-05: `IsAuthenticatedAsync()` called BEFORE `SignInAsync` — APPLIED
- T-03-06: `AuthenticatedPageModel.SafeApiCall` emits `Turbo-Visit-Control: reload` — APPLIED (Plan 01-02)
- T-03-07: JsonElement string-check extracts actual value from WhoAmI response — APPLIED

## Self-Check: PASSED

Files verified:
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Login.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Login.cshtml.cs` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Logout.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Logout.cshtml.cs` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Index.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Index.cshtml.cs` — FOUND

Commits verified: `d29644b`, `6fdcd18` — FOUND
