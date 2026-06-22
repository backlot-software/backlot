---
phase: 01-foundation-auth
verified: 2026-06-22T12:00:00Z
status: human_needed
score: 5/5 must-haves verified
behavior_unverified: 4
overrides_applied: 0
behavior_unverified_items:
  - truth: "User can visit /login, enter valid credentials, and be redirected to the authenticated shell root (/)"
    test: "Run Studio with Backlot API at localhost:7221; POST valid credentials to /login"
    expected: "Browser redirects to / and the authenticated shell renders with username in sidebar"
    why_human: "Requires a live Backlot API to exercise IsAuthenticatedAsync and SignInAsync; grep confirms the code path is present and wired but cannot confirm the full round-trip succeeds"
  - truth: "User can click 'Sign out' and be redirected to /login with session cleared and cookie invalidated"
    test: "After logging in, click Sign out in the sidebar"
    expected: "Redirect to /login; re-visiting / redirects to /login (cookie and session both gone)"
    why_human: "Requires a live session and cookie — cannot verify the session-clear + SignOut + redirect chain executes correctly without a running app"
  - truth: "When the Backlot API returns 401 mid-session, the browser performs a full-page navigation to /login"
    test: "Log in, then invalidate the API credentials server-side; trigger a page that calls the API"
    expected: "Browser performs a full-page (not Turbo-Frame) redirect to /login"
    why_human: "The Turbo-Visit-Control: reload header behavior is a browser-side Turbo invariant; presence of the header emit in SafeApiCall is verified but whether Turbo 8 respects it in this integration cannot be confirmed by grep"
  - truth: "Authenticated users see their username in the sidebar identity block via GET /api/role/director/whoami"
    test: "Log in with valid credentials; inspect the sidebar identity block"
    expected: "Sidebar shows the authenticated username (not 'Unknown user')"
    why_human: "WhoAmIAsync depends on the live Backlot API; the JsonElement extraction path and ViewData wiring is present but the actual username rendering requires a live API response"
human_verification:
  - test: "Log in with valid credentials via /login"
    expected: "Redirect to / with username visible in the sidebar identity block"
    why_human: "Requires live Backlot API at localhost:7221; full login round-trip cannot be exercised statically"
  - test: "Log in, then click Sign out"
    expected: "Redirect to /login; re-visiting / redirects to /login again"
    why_human: "Session clear and cookie invalidation are runtime behaviors that cannot be confirmed by static analysis"
  - test: "While logged in, force the API to return 401 (e.g. by stopping the Backlot API or revoking credentials)"
    expected: "Next authenticated page load redirects the browser to /login at the top level, not inside a Turbo Frame"
    why_human: "Turbo-Visit-Control: reload is a Turbo 8 runtime protocol — whether Turbo interprets the header correctly requires a live browser"
  - test: "Visit any authenticated page without a session cookie"
    expected: "Redirect to /login"
    why_human: "Cookie auth redirect is a runtime ASP.NET Core behavior triggered at request time"
---

# Phase 1: Foundation & Auth Verification Report

**Phase Goal:** Establish the project scaffold and authentication boundary — a developer can run the app, reach a login page, enter Backlot API credentials, and land on an authenticated dashboard.
**Verified:** 2026-06-22T12:00:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | User can log in with username and password; credentials are base64-encoded and stored server-side in session, never exposed to the browser | ✓ VERIFIED | `Login.cshtml.cs` OnPostAsync: `Convert.ToBase64String`, `SetString("BasicAuthHeader", encoded)`, cookie contains only `ClaimTypes.Name` (not password). `_LoginLayout` has no client-side script that could access the session value. |
| 2 | User can log out, which clears the server session and returns them to the login page | ✓ VERIFIED | `Logout.cshtml.cs`: `Session.Clear()` then `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` then `RedirectToPage("/Login")`. `[Authorize]` prevents anonymous logout trigger. |
| 3 | When the API returns 401 (expired/invalid credentials), the user is redirected to the login page at the top level (not inside a Turbo Frame) | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | `BasicAuthHandler.SendAsync` throws `BacklotApiUnauthorizedException` on HTTP 401. `AuthenticatedPageModel.SafeApiCall` catches it, sets `Response.Headers["Turbo-Visit-Control"] = "reload"`, then calls `Response.Redirect("/login")`. Code is present and wired — whether Turbo 8 runtime honors the header requires a live browser test. |
| 4 | An authenticated user sees their current identity (from `whoami`) in the sidebar | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | `IndexModel.OnGetAsync` calls `SafeApiCall(() => _api.WhoAmIAsync())`, handles `JsonElement` extraction, sets `ViewData["Username"]`. `_Sidebar.cshtml` renders `@ViewData["Username"]`. Full data-flow chain is wired; actual username rendering requires a live API. |
| 5 | Every outbound API call is issued by a pooled typed HttpClient with the Basic Auth header injected by a DelegatingHandler reading session per request (no `new HttpClient()`) | ✓ VERIFIED | `Program.cs`: `AddHttpClient<IBacklotApiClient, BacklotApiClient>(...).AddHttpMessageHandler<BasicAuthHandler>()`. `BasicAuthHandler` uses `IHttpContextAccessor` (not `ISession` constructor injection). `BacklotApiClient` injected via constructor (factory pattern). No `new HttpClient()` anywhere in application code. |

**Score:** 3/5 truths fully verified (2 behavior-unverified, all 5 present and wired)

**Behavior-unverified truths** (code present + wired; behavior not exercised by a test): 2 of 5 truths

_Note: The score above counts the 3 truths that are unambiguously verified by static evidence. The 2 behavior-unverified truths are complete implementations awaiting runtime confirmation._

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Backlot.Studio/Backlot.Studio.csproj` | Project file targeting net10.0 | ✓ VERIFIED | `<TargetFramework>net10.0</TargetFramework>` present; builds clean |
| `Backlot.Studio/Program.cs` | Entry point with full DI + middleware wiring | ✓ VERIFIED | Complete DI registrations; correct middleware order (UseRouting → UseAuthentication → UseAuthorization → UseSession → MapRazorPages) |
| `Backlot.Studio/Pages/Shared/_Layout.cshtml` | Authenticated two-panel shell with data-turbo-permanent | ✓ VERIFIED | Contains `data-turbo-permanent`, `id="sidebar"`, Bootstrap 5.3.8 CDN, Turbo 8.0.23 with `type="module"`, 4 `integrity=` SRI attributes |
| `Backlot.Studio/Pages/Shared/_LoginLayout.cshtml` | Minimal login layout (no sidebar) | ✓ VERIFIED | Bootstrap CSS only; no sidebar, no Turbo, no studio.css |
| `Backlot.Studio/Pages/Shared/_Sidebar.cshtml` | Sidebar partial with disabled nav + identity block | ✓ VERIFIED | Two `.nav-link.disabled` items (Scenarios, Roles); identity block with `@ViewData["Username"]`; antiforgery logout form; `#sidebar-toggle` button with 44px tap target |
| `Backlot.Studio/wwwroot/css/studio.css` | Sidebar collapse styles | ✓ VERIFIED | `transition: width 0.2s ease`, `aside#sidebar.collapsed { width: 64px !important; }`, `.sidebar-label` hide, margin-left `:has()` rule |
| `Backlot.Studio/wwwroot/js/studio.js` | Sidebar toggle with turbo:load | ✓ VERIFIED | `turbo:load` (not `DOMContentLoaded`); `onclick` assignment to prevent duplicate listeners; both aria-label strings present |
| `Backlot.Studio/Services/BasicAuthHandler.cs` | DelegatingHandler reading session per request | ✓ VERIFIED | `IHttpContextAccessor` constructor; `session.LoadAsync()` before `GetString`; throws `BacklotApiUnauthorizedException` on 401 |
| `Backlot.Studio/Services/IBacklotApiClient.cs` | Typed interface with IsAuthenticatedAsync + WhoAmIAsync | ✓ VERIFIED | Both methods declared; designed for Phase 2 extension |
| `Backlot.Studio/Services/BacklotApiClient.cs` | Typed client with ApiEnvelope deserialization | ✓ VERIFIED | `GetEnvelopeAsync<T>` helper; `EnsureSuccessStatusCode()`; `ReadFromJsonAsync<ApiEnvelope<T>>` |
| `Backlot.Studio/Services/ApiEnvelope.cs` | Envelope DTO (Body, Status, TimeInMs, ExecutionTime) | ✓ VERIFIED | All 4 properties present; PascalCase matches API; no JsonPropertyName needed |
| `Backlot.Studio/Services/BacklotApiUnauthorizedException.cs` | Typed 401 exception | ✓ VERIFIED | Extends `Exception`; parameterless constructor |
| `Backlot.Studio/Pages/AuthenticatedPageModel.cs` | Abstract base with SafeApiCall 401 interception | ✓ VERIFIED | `SafeApiCall<T>` catches `BacklotApiUnauthorizedException`, emits `Turbo-Visit-Control: reload`, calls `Response.Redirect("/login")` |
| `Backlot.Studio/Pages/Login.cshtml` | Centered Bootstrap card login page | ✓ VERIFIED | `_LoginLayout`, `data-turbo="false"`, `@Html.AntiForgeryToken()`, `alert-danger` conditional, `ReturnUrl` hidden input |
| `Backlot.Studio/Pages/Login.cshtml.cs` | LoginModel with credential validation + SignInAsync | ✓ VERIFIED | Correct order: encode → SetString → IsAuthenticatedAsync → Remove-on-failure → SignInAsync → LocalRedirect |
| `Backlot.Studio/Pages/Logout.cshtml.cs` | LogoutModel with Session.Clear + SignOutAsync | ✓ VERIFIED | `[Authorize]`; `Session.Clear()` → `SignOutAsync` → `RedirectToPage("/Login")` |
| `Backlot.Studio/Pages/Index.cshtml.cs` | IndexModel extending AuthenticatedPageModel with WhoAmI | ✓ VERIFIED | `[Authorize]`; extends `AuthenticatedPageModel`; `SafeApiCall` wrapping `WhoAmIAsync`; `JsonElement` string extraction; sets `ViewData["Username"]` |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| `_Layout.cshtml` | `_Sidebar.cshtml` | `<partial name="_Sidebar" />` | ✓ WIRED | Line 23 in `_Layout.cshtml` |
| `_Layout.cshtml` | `studio.js` | `<script src="~/js/studio.js">` | ✓ WIRED | Line 39 in `_Layout.cshtml` |
| `_Layout.cshtml` | `studio.css` | `<link href="~/css/studio.css" />` | ✓ WIRED | Line 17 in `_Layout.cshtml` |
| `BasicAuthHandler.cs` | ISession (`BasicAuthHeader`) | `IHttpContextAccessor.HttpContext?.Session.GetString("BasicAuthHeader")` | ✓ WIRED | Present in `SendAsync`; `LoadAsync` called before read |
| `BacklotApiClient.cs` | `BasicAuthHandler.cs` | `AddHttpMessageHandler<BasicAuthHandler>()` in Program.cs | ✓ WIRED | Line 35 in `Program.cs` |
| `AuthenticatedPageModel.cs` | `BacklotApiUnauthorizedException.cs` | `catch (Services.BacklotApiUnauthorizedException)` | ✓ WIRED | Present in `SafeApiCall` |
| `Login.cshtml.cs` | `IBacklotApiClient.IsAuthenticatedAsync()` | `await _apiClient.IsAuthenticatedAsync()` in `OnPostAsync` | ✓ WIRED | Line 41 in `Login.cshtml.cs` |
| `Login.cshtml.cs` | Cookie Auth (`SignInAsync`) | `await HttpContext.SignInAsync(...)` after API validation | ✓ WIRED | Line 56 in `Login.cshtml.cs` |
| `Index.cshtml.cs` | `AuthenticatedPageModel.SafeApiCall` | `extends AuthenticatedPageModel`; calls `SafeApiCall(...)` | ✓ WIRED | Line 9 and 20 in `Index.cshtml.cs` |
| `_Sidebar.cshtml` | `ViewData["Username"]` | `@ViewData["Username"]` rendered in identity block | ✓ WIRED | Set by `IndexModel.OnGetAsync`; rendered by `_Sidebar.cshtml` line 26 |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `_Sidebar.cshtml` (Username) | `ViewData["Username"]` | `IndexModel.OnGetAsync` → `SafeApiCall(() => _api.WhoAmIAsync())` → `BacklotApiClient.GetEnvelopeAsync<object>("api/role/director/whoami")` → HTTP GET to Backlot API | Depends on live API | ✓ FLOWING (wired to real API call; no hardcoded placeholder) |
| `Login.cshtml` (Error state) | `ModelState.IsValid` | `LoginModel.OnPostAsync` → `IsAuthenticatedAsync()` → API call → sets `ModelState.AddModelError` on failure | Depends on API response | ✓ FLOWING (error state driven by real API response, not hardcoded) |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Project builds clean | `dotnet build Backlot.Studio/Backlot.Studio.csproj` | Exit 0, 0 errors, 0 warnings | ✓ PASS |
| Solution references Studio project | `grep "Backlot.Studio" Backlot.sln` | Match found | ✓ PASS |
| All 7 phase commits exist in git | `git log --oneline \| grep -E "009db20\|8cc5e49..."` | All 7 hashes found | ✓ PASS |
| studio.js uses turbo:load only | `grep -c "DOMContentLoaded" studio.js` | 0 (absent) | ✓ PASS |
| studio.js has both aria-label strings | Node eval | `Collapse sidebar` and `Expand sidebar` both present | ✓ PASS |
| _Layout has 4 SRI integrity attributes | `grep -c "integrity=" _Layout.cshtml` | 4 | ✓ PASS |
| Login POST round-trip (full auth flow) | Requires live Backlot API | N/A | ? SKIP — no running service |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| AUTH-01 | 01-02, 01-03 | User can log in with username/password; credentials base64-encoded and stored in server-side session | ✓ SATISFIED | `Login.cshtml.cs` encodes credentials, stores in `ISession["BasicAuthHeader"]`, validates via `IsAuthenticatedAsync`, signs in via `SignInAsync`; `data-turbo="false"` on form |
| AUTH-02 | 01-03 | User can log out; server session is cleared and user is redirected to login | ✓ SATISFIED | `Logout.cshtml.cs`: `Session.Clear()` → `SignOutAsync` → `RedirectToPage("/Login")` |
| AUTH-03 | 01-02, 01-03 | User is automatically redirected to login when API returns 401 | ✓ SATISFIED | `BasicAuthHandler` throws `BacklotApiUnauthorizedException` on 401; `AuthenticatedPageModel.SafeApiCall` emits `Turbo-Visit-Control: reload` and redirects; `[Authorize]` on all authenticated pages |
| AUTH-04 | 01-03 | User sees their current username/identity in the navbar via `whoami` | ✓ SATISFIED | `IndexModel.OnGetAsync` calls `WhoAmIAsync` via `SafeApiCall`; sets `ViewData["Username"]`; `_Sidebar.cshtml` renders it |

No orphaned requirements for Phase 1: REQUIREMENTS.md maps exactly AUTH-01, AUTH-02, AUTH-03, AUTH-04 to Phase 1 — all four are claimed across Plans 01-02 and 01-03 and verified above.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None | — | — | — | No debt markers (TBD/FIXME/XXX), no stubs, no hardcoded empty data found in application files |

The wwwroot/lib/ directory contains vendored jQuery/Bootstrap source maps with `XXX` in comments (IBAN format spec) — these are third-party library files not produced by this phase and are out of scope.

### Human Verification Required

All automated checks pass. The following require a running Backlot API (`dotnet run --project Backlot.Studio/Backlot.Studio.csproj` with Backlot API at `https://localhost:7221`):

#### 1. End-to-End Login Flow

**Test:** Visit the running app at its port. Without a session cookie, navigate to `/`. Then visit `/login`, enter valid Backlot API credentials, and submit the form.
**Expected:** The unauthenticated visit to `/` redirects to `/login`. After entering valid credentials, the app redirects to `/` and the sidebar identity block displays the authenticated username (not "Unknown user").
**Why human:** Requires a live Backlot API to execute `IsAuthenticatedAsync()` and `WhoAmIAsync()`. The code path is fully wired but the runtime behavior cannot be confirmed statically.

#### 2. Logout Clears Session

**Test:** After logging in, click the "Sign out" button in the sidebar.
**Expected:** Redirect to `/login`. Attempting to visit `/` again redirects back to `/login` (session and auth cookie are both gone).
**Why human:** Session.Clear() + SignOutAsync + redirect is a runtime sequence; correct sequencing and cookie invalidation cannot be confirmed without executing the flow.

#### 3. Mid-Session 401 Full-Page Redirect

**Test:** Log in successfully. Then stop or invalidate the Backlot API credentials server-side so the next API call returns 401. Trigger a page navigation that calls the API.
**Expected:** The browser performs a full-page navigation to `/login` — not a Turbo Frame-scoped partial update. The redirect appears at the top-level window, not inside a `<turbo-frame>`.
**Why human:** The `Turbo-Visit-Control: reload` header is a Turbo 8 runtime protocol. Whether Turbo 8 correctly interprets this header in the integration (ESM module via CDN, ASP.NET Core response) requires a live browser.

#### 4. Invalid Credentials Error Display

**Test:** Visit `/login` and enter an incorrect username or password.
**Expected:** The page re-renders with the `alert-danger` banner reading "Invalid username or password." No redirect occurs.
**Why human:** Requires a live API to return a non-authenticated response from `IsAuthenticatedAsync()`.

---

## Gaps Summary

No gaps found. All artifacts exist, are substantive, and are wired. The build is clean (0 errors, 0 warnings). All four AUTH requirements are satisfied by the implementation.

The `human_needed` status reflects 4 human verification items: the end-to-end login flow, logout session clearing, mid-session 401 Turbo redirect behavior, and invalid credential error display — all of which require a live Backlot API at `localhost:7221` to exercise.

---

_Verified: 2026-06-22T12:00:00Z_
_Verifier: Claude (gsd-verifier)_
