# Phase 1: Foundation & Auth — Pattern Map

**Mapped:** 2026-06-22
**Files analyzed:** 12 new files
**Analogs found:** 1 / 12 (codebase is greenfield — no Razor Pages exist anywhere in the solution)

## Situation Note

Phase 1 is the first code committed to `Backlot.Studio/`. The directory currently contains only `openapidoc.json` and planning artifacts. No `.csproj`, no `Program.cs`, no Razor Pages, and no services exist yet. `Backlot.Demo.Web` is a Backlot API host (Minimal API via `ApplicationBuilding.cs`) — not a Razor Pages app — so it offers no direct page/service analogs.

**Consequence:** All patterns below come from:
1. `01-RESEARCH.md` — code examples cited from official Microsoft Learn (HIGH-confidence; exact API signatures confirmed for .NET 10)
2. `Backlot.WebApp/ApplicationBuilding.cs` — real DI wiring pattern (service registration style used in this solution)
3. Phase 2 `02-PATTERNS.md` — already-established canonical patterns for Studio files that Phase 1 creates (Layout, Sidebar, BacklotApiClient, studio.js, studio.css). Phase 1 must produce files consistent with those Phase 2 references.

The planner must treat all patterns below as **first-instance patterns** — they define the canonical style this project will follow going forward, not copies of existing code.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Backlot.Studio.csproj` | config | — | none | no analog |
| `Program.cs` | config/bootstrap | request-response | `Backlot.WebApp/ApplicationBuilding.cs` | partial (DI/middleware wiring style) |
| `appsettings.json` | config | — | `Backlot.Demo.Web/appsettings.json` | role-match |
| `Pages/Shared/_Layout.cshtml` | layout | request-response | none (Phase 2 defines canonical form) | no analog — use RESEARCH.md Pattern 5 + 02-PATTERNS.md |
| `Pages/Shared/_LoginLayout.cshtml` | layout | request-response | none | no analog |
| `Pages/Shared/_Sidebar.cshtml` | partial view | request-response | none (Phase 2 defines canonical form) | no analog — use 02-PATTERNS.md |
| `Pages/Login.cshtml` | view | request-response | none | no analog |
| `Pages/Login.cshtml.cs` | page model | request-response | none | no analog — use RESEARCH.md Pattern 3 |
| `Pages/Logout.cshtml.cs` | page model | request-response | none | no analog — use RESEARCH.md Pattern 3 |
| `Pages/Index.cshtml` | view | request-response | none | no analog |
| `Pages/Index.cshtml.cs` | page model | request-response | none | no analog — use RESEARCH.md Code Examples |
| `Services/IBacklotApiClient.cs` | service interface | request-response | none | no analog |
| `Services/BacklotApiClient.cs` | service | request-response | `Backlot.WebApp/ApplicationBuilding.cs` | partial (DI wiring only) |
| `Services/BasicAuthHandler.cs` | middleware/handler | request-response | none | no analog — use RESEARCH.md Pattern 1 |
| `Services/ApiEnvelope.cs` | model/DTO | transform | `Backlot.Http/Media/Formatters/JsonResponse.cs` | shape-match (property names verified) |
| `Services/BacklotApiUnauthorizedException.cs` | utility | — | none | no analog |
| `wwwroot/css/studio.css` | stylesheet | — | none | no analog — use UI-SPEC |
| `wwwroot/js/studio.js` | client utility | event-driven | none (Phase 2 defines canonical form) | no analog — use 02-PATTERNS.md |

---

## Pattern Assignments

### `Program.cs` (config/bootstrap, request-response)

**Analog source:** `Backlot.WebApp/ApplicationBuilding.cs` lines 1–57 (DI service registration style)

**Imports pattern** — solution uses top-level using statements; no namespace declaration on `Program.cs`:
```csharp
// Source: Backlot.Demo.Web/Program.cs (lines 1-8) — top-level statement style used in this solution
using Microsoft.AspNetCore.Authentication.Cookies;
```

**Middleware order pattern** (CRITICAL — from RESEARCH.md Pattern 2, confirmed by ASP.NET Core docs):
```csharp
// Source: 01-RESEARCH.md §Pattern 2 — "Middleware order (CRITICAL — UseSession must come after UseRouting)"
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();      // must come before UseAuthorization
app.UseAuthorization();
app.UseSession();             // after UseRouting; before MapRazorPages
app.MapRazorPages();
```

**Session + Cookie Auth DI registration** (from RESEARCH.md Pattern 2 + 3):
```csharp
// Source: 01-RESEARCH.md §Pattern 2 and §Pattern 3
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);       // D-04/D-05
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // must match session IdleTimeout
        options.SlidingExpiration = true;                 // D-05
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BasicAuthHandler>();

builder.Services.AddHttpClient<IBacklotApiClient, BacklotApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BacklotApi:BaseUrl"]
        ?? "https://localhost:7221");
}).AddHttpMessageHandler<BasicAuthHandler>();

builder.Services.AddRazorPages();
```

---

### `Services/BasicAuthHandler.cs` (middleware/handler, request-response)

**Analog:** None in codebase. Full pattern from RESEARCH.md Pattern 1.

**Imports pattern:**
```csharp
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
```

**Core handler pattern** (from RESEARCH.md Pattern 1 — IHttpContextAccessor, NOT ISession directly):
```csharp
// Source: 01-RESEARCH.md §Pattern 1
// CRITICAL: Never inject ISession directly — scoped service in singleton-lifetime handler pool causes ObjectDisposedException.
public class BasicAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BasicAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var basicAuthHeader = session?.GetString("BasicAuthHeader");

        if (!string.IsNullOrEmpty(basicAuthHeader))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", basicAuthHeader);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new BacklotApiUnauthorizedException();
        }

        return response;
    }
}
```

**Anti-pattern to avoid:** `ISession` injected into constructor — throws `ObjectDisposedException` under load. Always use `IHttpContextAccessor` and read `HttpContext?.Session` inside `SendAsync`.

---

### `Services/BacklotApiClient.cs` + `Services/IBacklotApiClient.cs` (service, request-response)

**Analog source (DI style):** `Backlot.WebApp/ApplicationBuilding.cs` lines 33–43

**Imports pattern:**
```csharp
using System.Net.Http.Json;
using Backlot.Studio.Services;
```

**Core method pattern** (from RESEARCH.md Code Examples):
```csharp
// Source: 01-RESEARCH.md §Code Examples — Envelope Deserialization
private async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
{
    var response = await _httpClient.GetAsync(path, ct);
    response.EnsureSuccessStatusCode();
    var envelope = await response.Content
        .ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
    return envelope?.Body;
}

// IsAuthenticatedAsync — called from Login.cshtml.cs to validate credentials
public async Task<bool> IsAuthenticatedAsync()
{
    var result = await GetAsync<bool>("api/role/director/isauthenticated");
    return result;
}

// WhoAmIAsync — called server-side from authenticated PageModels
public async Task<object?> WhoAmIAsync()
{
    return await GetAsync<object>("api/role/director/whoami");
}
```

**Interface pattern:**
```csharp
// Services/IBacklotApiClient.cs
public interface IBacklotApiClient
{
    Task<bool> IsAuthenticatedAsync();
    Task<object?> WhoAmIAsync();
}
```

---

### `Services/ApiEnvelope.cs` (model/DTO, transform)

**Analog source:** `Backlot.Http/Media/Formatters/JsonResponse.cs` (property names verified)

The API wraps every response in an envelope. Property names are PascalCase — `System.Text.Json` defaults match with case-insensitive deserialization via `ReadFromJsonAsync`.

```csharp
// Source: Backlot.Http/Media/Formatters/JsonResponse.cs — verified property names
// Studio public DTO version (copy this shape exactly — consistent with 02-PATTERNS.md)
namespace Backlot.Studio.Services;

public class ApiEnvelope<T>
{
    public T? Body { get; set; }
    public string? Status { get; set; }
    public long TimeInMs { get; set; }
    public DateTimeOffset ExecutionTime { get; set; }
}
```

---

### `Services/BacklotApiUnauthorizedException.cs` (utility)

Simple typed exception — no analog needed:
```csharp
namespace Backlot.Studio.Services;

public class BacklotApiUnauthorizedException : Exception
{
    public BacklotApiUnauthorizedException()
        : base("The Backlot API returned 401 Unauthorized.") { }
}
```

---

### `Pages/Login.cshtml.cs` (page model, request-response)

**Analog:** None in codebase. Full pattern from RESEARCH.md Pattern 3.

**Imports pattern:**
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Backlot.Studio.Services;
```

**Core OnPost pattern** (from RESEARCH.md Pattern 3 — validate BEFORE SignIn):
```csharp
// Source: 01-RESEARCH.md §Pattern 3 Login handler
// CRITICAL: Sign in AFTER API validates credentials — not before.
public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid) return Page();

    // 1. Build Basic Auth header value (stored WITHOUT "Basic " prefix)
    var raw = $"{Input.Username}:{Input.Password}";
    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));

    // 2. Temporarily store to allow BasicAuthHandler to inject it
    HttpContext.Session.SetString("BasicAuthHeader", encoded);
    var isValid = await _apiClient.IsAuthenticatedAsync();
    if (!isValid)
    {
        HttpContext.Session.Remove("BasicAuthHeader");
        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        return Page();
    }

    // 3. Sign in cookie auth (after API confirms credentials)
    var claims = new List<Claim> { new Claim(ClaimTypes.Name, Input.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return LocalRedirect(ReturnUrl ?? "/");
}
```

**Input model pattern:**
```csharp
public class LoginInputModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
```

---

### `Pages/Login.cshtml` (view, request-response)

**Analog:** None. Pattern from UI-SPEC §Layout Contract — Login Page and RESEARCH.md Pattern 5.

**Layout directive** (uses `_LoginLayout`, NOT `_Layout` — no sidebar on login page per D-06):
```cshtml
@{
    Layout = "_LoginLayout";
    ViewData["Title"] = "Sign in — Backlot Studio";
}
```

**Critical Turbo opt-out** (from RESEARCH.md Pitfall 5):
```html
<!-- data-turbo="false" prevents Turbo Drive from intercepting the POST -->
<!-- Without this, cookie Set-Cookie headers from the redirect may not be applied correctly -->
<form method="post" asp-page="/Login" data-turbo="false">
    @Html.AntiForgeryToken()
```

**Error state** (from UI-SPEC §States — alert above form fields, per D-07):
```html
@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger mt-3" role="alert">
        Invalid username or password.
    </div>
}
```

**Card layout** (from UI-SPEC §Layout Contract):
```html
<div class="d-flex justify-content-center align-items-center min-vh-100">
    <div class="card p-4" style="max-width:400px; width:100%">
        <h4 class="text-center fw-semibold mb-4">Backlot Studio</h4>
        <!-- error alert here -->
        <!-- form fields here -->
        <button type="submit" class="btn btn-primary w-100">Sign in</button>
    </div>
</div>
```

---

### `Pages/Logout.cshtml.cs` (page model, request-response)

**Analog:** None. Full pattern from RESEARCH.md Pattern 3.

```csharp
// Source: 01-RESEARCH.md §Pattern 3 Logout handler
// Prefer POST to prevent CSRF-triggered logout via <img> or GET link.
public async Task<IActionResult> OnPostAsync()
{
    HttpContext.Session.Clear();
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToPage("/Login");
}
```

---

### `Pages/Index.cshtml.cs` (page model, request-response)

**Analog:** None. Pattern from RESEARCH.md Code Examples — Whoami.

Extends the `AuthenticatedPageModel` base class for 401 safety:
```csharp
// Source: 01-RESEARCH.md §Code Examples — Whoami
[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    public IndexModel(IBacklotApiClient api) => _api = api;

    public async Task<IActionResult> OnGetAsync()
    {
        var username = await SafeApiCall(async () =>
        {
            var result = await _api.WhoAmIAsync();
            return result?.ToString() ?? "Unknown user";
        });
        ViewData["Username"] = username ?? "Unknown user";
        return Page();
    }
}
```

---

### `Pages/Shared/_Layout.cshtml` (layout, request-response)

**Consistent with 02-PATTERNS.md** — Phase 1 creates this file; Phase 2 extends it. Use the CDN block and two-panel shell from 02-PATTERNS.md (minus the Scalar panel which is Phase 2's concern).

**Phase 1 head block** (Scalar CDN tag is OMITTED in Phase 1 — added in Phase 2):
```html
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — Backlot Studio</title>
    <!-- Bootstrap 5.3.8 — SRI hash: fetch from jsDelivr at implementation time -->
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css"
          crossorigin="anonymous" />
    <!-- Bootstrap Icons 1.13.1 -->
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css"
          crossorigin="anonymous" />
    <link rel="stylesheet" href="~/css/studio.css" />
</head>
```

**Two-panel shell** (from UI-SPEC §Layout Contract — sidebar 240px fixed, main flex-grow):
```html
<body>
<div class="d-flex" style="min-height:100vh">
    <aside id="sidebar" data-turbo-permanent
           style="width:240px; flex-shrink:0; position:fixed; top:0; left:0; height:100vh;">
        <partial name="_Sidebar" />
    </aside>
    <main class="flex-grow-1 p-4" style="margin-left:240px">
        @RenderBody()
    </main>
</div>

<!-- Hotwired Turbo 8.0.23 — after body so DOM is parsed first -->
<script src="https://cdn.jsdelivr.net/npm/@hotwired/turbo@8.0.23/dist/turbo.es2017.esm.js"
        type="module" crossorigin="anonymous"></script>
<!-- Bootstrap 5.3.8 JS bundle -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js"
        crossorigin="anonymous"></script>
<script src="~/js/studio.js"></script>
@await RenderSectionAsync("Scripts", required: false)
</body>
```

**CRITICAL:** `<aside id="sidebar" data-turbo-permanent>` — the `id` attribute is mandatory for `data-turbo-permanent` to work. Without it, Turbo cannot match the element across page loads and collapse state resets on every navigation (RESEARCH.md Pitfall 4).

---

### `Pages/Shared/_LoginLayout.cshtml` (layout, request-response)

Minimal layout for the login page — no sidebar, no CDN script references except Bootstrap:
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"]</title>
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css"
          crossorigin="anonymous" />
</head>
<body>
    @RenderBody()
</body>
</html>
```

---

### `Pages/Shared/_Sidebar.cshtml` (partial view, request-response)

**Consistent with 02-PATTERNS.md** — Phase 1 creates this file with placeholder disabled nav items (D-02).

**Phase 1 version** (both nav items disabled; Phase 2 activates Scenarios):
```html
@{
    var activeNav = ViewData["ActiveNav"] as string ?? "";
}
<nav class="d-flex flex-column h-100 p-3" style="background: var(--bs-tertiary-bg)">
    <!-- App name — hidden in collapsed icon-rail mode via CSS -->
    <div class="fw-semibold mb-4 px-1 sidebar-label">Backlot Studio</div>

    <!-- Nav items -->
    <ul class="nav nav-pills flex-column mb-auto gap-1">
        <li class="nav-item">
            <span class="nav-link disabled text-muted" aria-disabled="true">
                <i class="bi bi-play-circle me-2"></i>
                <span class="sidebar-label">Scenarios</span>
            </span>
        </li>
        <li class="nav-item">
            <span class="nav-link disabled text-muted" aria-disabled="true">
                <i class="bi bi-boxes me-2"></i>
                <span class="sidebar-label">Roles</span>
            </span>
        </li>
    </ul>

    <!-- Identity block (bottom) -->
    <div class="border-top pt-3 mt-3">
        <small class="text-muted d-block sidebar-label">@ViewData["Username"]</small>
        <form method="post" action="/logout" class="mt-1">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-link p-0 text-muted small">Sign out</button>
        </form>
    </div>

    <!-- Toggle button -->
    <button id="sidebar-toggle" class="btn btn-link p-2 mt-2 text-muted"
            aria-label="Collapse sidebar"
            style="min-height:44px; min-width:44px">
        <i class="bi bi-list fs-5"></i>
    </button>
</nav>
```

**Logout uses POST form with antiforgery** — not a plain `<a>` link — to prevent CSRF-triggered logout (RESEARCH.md §Interaction Contract).

---

### `wwwroot/js/studio.js` (client utility, event-driven)

**Consistent with 02-PATTERNS.md** — Phase 1 creates a minimal version; Phase 2 adds Scalar panel logic.

**Phase 1 version** (sidebar toggle only — no Scalar):
```javascript
// Source: 02-PATTERNS.md §studio.js (Phase 1 subset — sidebar toggle only)
// Use turbo:load, not DOMContentLoaded, so it runs after every Turbo Drive navigation.
document.addEventListener('turbo:load', function () {
    const toggle = document.getElementById('sidebar-toggle');
    const sidebar = document.getElementById('sidebar');
    if (!toggle || !sidebar) return;

    // Restore aria-label based on current state
    const collapsed = sidebar.classList.contains('collapsed');
    toggle.setAttribute('aria-label', collapsed ? 'Expand sidebar' : 'Collapse sidebar');
});

document.getElementById('sidebar-toggle')?.addEventListener('click', function () {
    const sidebar = document.getElementById('sidebar');
    if (!sidebar) return;
    sidebar.classList.toggle('collapsed');
    const isCollapsed = sidebar.classList.contains('collapsed');
    this.setAttribute('aria-label', isCollapsed ? 'Expand sidebar' : 'Collapse sidebar');
    // Icon switch handled via CSS targeting sidebar.collapsed #sidebar-toggle .bi
});
```

**Note:** `data-turbo-permanent` on the sidebar means sidebar DOM is preserved across navigations — the `turbo:load` listener only needs to restore aria-label state, not the class itself.

---

### `wwwroot/css/studio.css` (stylesheet)

**Phase 1 version** (sidebar collapse only — Scalar panel styles added in Phase 2):
```css
/* wwwroot/css/studio.css — Phase 1 */

/* Sidebar width transition */
aside#sidebar {
    transition: width 0.2s ease;
}

/* Collapsed icon-rail state */
aside#sidebar.collapsed {
    width: 64px !important;
}

/* Hide text labels in collapsed mode */
aside#sidebar.collapsed .sidebar-label {
    display: none;
}

/* Adjust main content margin when sidebar is collapsed */
body:has(aside#sidebar.collapsed) main {
    margin-left: 64px !important;
}
```

**Note:** `body:has(aside#sidebar.collapsed)` is a CSS `:has()` selector — supported in all modern browsers (Chromium 105+, Firefox 121+, Safari 15.4+). For Phase 1 / developer tool context this is acceptable.

---

### `Pages/Index.cshtml` (view, authenticated shell root)

Minimal Phase 1 placeholder — the shell root. Phase 2 will redirect this or replace it:
```cshtml
@page
@model Backlot.Studio.Pages.IndexModel
@{
    ViewData["Title"] = "Backlot Studio";
    ViewData["ActiveNav"] = "";
}

<h4 class="fw-semibold mb-3">Welcome to Backlot Studio</h4>
<p class="text-muted">Logged in as <strong>@ViewData["Username"]</strong>.</p>
```

---

### `AuthenticatedPageModel` base class (utility page model)

All authenticated PageModels inherit this to get safe 401 handling (RESEARCH.md Pattern 4):
```csharp
// Source: 01-RESEARCH.md §Pattern 4 — AuthenticatedPageModel base
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Pages;

public abstract class AuthenticatedPageModel : PageModel
{
    protected async Task<T?> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return await apiCall();
        }
        catch (Services.BacklotApiUnauthorizedException)
        {
            // Turbo-safe full-page redirect — not a frame-scoped redirect
            Response.Headers["Turbo-Visit-Control"] = "reload";
            Response.Redirect("/login");
            return default;
        }
    }
}
```

---

## Shared Patterns

### Authentication Guard
**Source:** RESEARCH.md Pattern 3 / `[Authorize]` + cookie auth in `Program.cs`
**Apply to:** All PageModel classes EXCEPT `Login.cshtml.cs` and `Logout.cshtml.cs`
```csharp
[Authorize]
public class IndexModel : AuthenticatedPageModel { ... }
```

### Antiforgery on All POST Forms
**Source:** RESEARCH.md §Security Domain — CSRF
**Apply to:** Login form, Logout form, any future POST handler
```html
<!-- Razor Pages auto-validates antiforgery from asp-page forms -->
<!-- Explicitly include @Html.AntiForgeryToken() in any plain <form method="post"> -->
<form method="post" action="/logout">
    @Html.AntiForgeryToken()
    ...
</form>
```

### ViewData Username Convention
**Source:** RESEARCH.md Code Examples — Whoami, 02-PATTERNS.md §ViewData Active Nav Convention
**Apply to:** All authenticated PageModel `OnGetAsync` methods
```csharp
// Load username server-side on every authenticated page load
// Pass via ViewData so _Sidebar.cshtml and _Layout.cshtml can read it
ViewData["Username"] = username ?? "Unknown user";
```

### API Error Handling
**Source:** 02-PATTERNS.md §Shared Patterns — API Error Handling
**Apply to:** All `OnGetAsync` / `OnPostAsync` that call `IBacklotApiClient`
```csharp
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    ErrorMessage = "Could not reach the Backlot API. Check that it is running.";
}
// Do NOT catch BacklotApiUnauthorizedException here — let it propagate to SafeApiCall
```

### CDN Version Pinning + SRI
**Source:** 01-RESEARCH.md §Package Legitimacy Audit
**Apply to:** All `<link>` and `<script>` CDN tags in `_Layout.cshtml` and `_LoginLayout.cshtml`

Fetch SRI hashes from jsDelivr at implementation time:
- https://www.jsdelivr.com/package/npm/bootstrap?version=5.3.8
- https://www.jsdelivr.com/package/npm/bootstrap-icons?version=1.13.1
- https://www.jsdelivr.com/package/npm/@hotwired/turbo?version=8.0.23

---

## No Analog Found

All Phase 1 files are first-instance patterns. No existing Razor Pages, typed HTTP clients, or DelegatingHandlers exist in the Backlot solution.

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `Backlot.Studio.csproj` | config | — | No Razor Pages `.csproj` exists in solution |
| `Program.cs` | bootstrap | request-response | No Razor Pages `Program.cs` exists; Demo.Web uses Backlot framework wiring |
| `Pages/Login.cshtml` + `.cs` | view + page model | request-response | No Razor Pages in solution |
| `Pages/Logout.cshtml.cs` | page model | request-response | No Razor Pages in solution |
| `Pages/Index.cshtml` + `.cs` | view + page model | request-response | No Razor Pages in solution |
| `Pages/Shared/_Layout.cshtml` | layout | — | No Razor Pages in solution |
| `Pages/Shared/_LoginLayout.cshtml` | layout | — | No Razor Pages in solution |
| `Pages/Shared/_Sidebar.cshtml` | partial | — | No Razor Pages in solution |
| `Services/BasicAuthHandler.cs` | handler | request-response | No DelegatingHandler in solution |
| `Services/BacklotApiClient.cs` | service | request-response | No typed HTTP client in solution |
| `wwwroot/js/studio.js` | utility | event-driven | No client JS in Studio |
| `wwwroot/css/studio.css` | stylesheet | — | No CSS in Studio |

**The `ApiEnvelope<T>` shape IS verified against real Backlot source** — `Backlot.Http/Media/Formatters/JsonResponse.cs` confirms PascalCase property names (`Body`, `Status`, `TimeInMs`, `ExecutionTime`).

---

## Consistency Note for Planner

Phase 2 `02-PATTERNS.md` references files that Phase 1 creates (`_Layout.cshtml`, `_Sidebar.cshtml`, `studio.js`, `studio.css`, `BacklotApiClient`). The Phase 1 planner must ensure:

1. `_Layout.cshtml` shell structure (two-panel, `<aside id="sidebar" data-turbo-permanent>`) matches what Phase 2 extends with the Scalar panel.
2. `BacklotApiClient` / `IBacklotApiClient` interface is designed to support `GetScenariosAsync` being added in Phase 2 without breaking the registration.
3. `studio.js` uses `turbo:load` event listeners (not `DOMContentLoaded`) — Phase 2 appends Scalar init to this same file.
4. `studio.css` sidebar collapse styles are in a dedicated section so Phase 2 can append Scalar panel styles without conflict.

---

## Metadata

**Analog search scope:** `/home/jeroen/Projects/Backlot/Backlot.Studio/`, `/home/jeroen/Projects/Backlot/Backlot.Demo.Web/`, `/home/jeroen/Projects/Backlot/Backlot.WebApp/`, `/home/jeroen/Projects/Backlot/Backlot.Http/`
**Files scanned:** 30+
**Razor Pages found:** 0
**DelegatingHandlers found:** 0
**Primary pattern sources:** `01-RESEARCH.md` (HIGH confidence), `Backlot.Http/Media/Formatters/JsonResponse.cs` (verified shape), `02-PATTERNS.md` (canonical Phase 2 forward-compatibility reference)
**Pattern extraction date:** 2026-06-22
