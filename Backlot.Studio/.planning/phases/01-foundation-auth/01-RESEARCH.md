# Phase 1: Foundation & Auth — Research

**Researched:** 2026-06-22
**Domain:** ASP.NET Core Razor Pages, typed HttpClient, session-based Basic Auth, cookie auth gating, Turbo Drive
**Confidence:** MEDIUM

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Use a **left sidebar** layout (not top navbar). Two-panel: fixed sidebar + main content area via Bootstrap flex.
- **D-02:** Sidebar contains **placeholder nav items** (disabled/grayed links) for Scenarios and Roles in Phase 1, before those pages exist. Each phase activates its own nav entry when it ships.
- **D-03:** Sidebar is **collapsible** via a toggle button (icon-only collapsed mode). JS needed to manage toggle state; full width collapses to icon rail.
- **D-04:** Session idle timeout is **8 hours** (matches a workday for a developer tool used at a desk).
- **D-05:** Session uses **sliding expiry** — timeout resets on each request. Active use stays logged in; idle for 8 hours triggers re-login.
- **D-06:** Login page is a **centered Bootstrap card** with Studio branding (app title at top, username + password fields, login button). Full-page centered layout — no sidebar visible on the login page.
- **D-07:** Wrong-credentials error is shown as a **Bootstrap `alert-danger` banner above the form** with the message "Invalid username or password." No field-level highlighting.
- **D-08:** Authenticated user identity is shown **in the sidebar** (top or bottom section), displaying **username only** with a logout link beneath it.
- **D-09:** Only the **username** is shown — no role/type label. Data source: `GET /api/role/director/whoami`.

### Claude's Discretion
- Exact sidebar collapse animation (CSS transition duration, icon used for toggle).
- Whether sidebar toggle state persists across Turbo navigations (use `data-turbo-permanent` on the sidebar element to preserve state).
- Bootstrap color theme (light/dark mode default).
- Exact wording of the "Backlot Studio" branding on the login card.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AUTH-01 | User can log in with username and password; credentials are base64-encoded as `username:password` and stored in server-side session as a Basic Auth header | Session config pattern + cookie auth SignInAsync |
| AUTH-02 | User can log out; server session is cleared and user is redirected to the login page | HttpContext.SignOutAsync + session.Clear() + redirect |
| AUTH-03 | User is automatically redirected to the login page when the Backlot API returns a 401 | BasicAuthHandler detecting 401 response + top-level redirect strategy |
| AUTH-04 | User sees their current username/identity in the navbar via `GET /api/role/director/whoami` | whoami called server-side in PageModel OnGetAsync, passed via ViewData |
</phase_requirements>

---

## Summary

Phase 1 is a greenfield ASP.NET Core 10 Razor Pages project that implements three capabilities: project scaffolding, a typed API service layer with session-backed Basic Auth, and the login/logout/401 auth boundary. All three capabilities are server-rendered with no client JS build pipeline. The core technical challenge is the `BasicAuthHandler` pattern: a `DelegatingHandler` must read per-request session credentials and inject a `Basic` Authorization header on every outbound `HttpClient` call, without violating `IHttpClientFactory`'s handler-pooling model.

The second non-obvious challenge is the 401-to-top-level-redirect requirement. When the Backlot API returns 401 to a page that was loaded inside a Turbo Frame, a plain `Response.Redirect()` only refreshes the frame, not the whole page. The solution is to emit a `Turbo-Visit-Control: reload` HTTP response header or include `<meta name="turbo-visit-control" content="reload">` in the response, which instructs Turbo Drive to perform a full-page navigation regardless of how the response was triggered.

The third element is the two-layer credential system: a cookie auth scheme gates access to Razor Pages (triggering `[Authorize]` redirects to `/login`), while an entirely separate `ISession` stores the actual Basic Auth credentials sent to the Backlot API. These two layers serve different purposes and must be kept distinct. The cookie layer is a lightweight identity presence signal for Razor Pages routing; the session layer is the credential vault for outbound API calls.

**Primary recommendation:** Implement `BasicAuthHandler` as a `Transient` `DelegatingHandler` injecting `IHttpContextAccessor`. Read `HttpContext?.Session` in `SendAsync` — never in the constructor. Store `Authorization` header value as a session string key `"BasicAuthHeader"`. Sign in with a minimal `ClaimsPrincipal` containing only the username claim. Set both cookie auth and session `IdleTimeout` to 8 hours with `SlidingExpiration = true`.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Auth gating (login/logout/redirect) | Frontend Server (Razor Pages) | — | Cookie auth + `[Authorize]` is a server-side concern; the browser holds only a signed cookie |
| Credential storage (Basic Auth header) | Frontend Server (session store) | — | Must never touch the browser; `ISession` is server-side memory cache |
| API credential injection | Frontend Server (DelegatingHandler) | — | The handler executes in the server process on every outbound call |
| 401 detection and redirect | Frontend Server (middleware / handler) | — | Server detects API 401, issues server-side redirect with Turbo reload signal |
| Identity display (whoami) | Frontend Server (PageModel OnGetAsync) | — | Called server-side, passed to layout via ViewData; no client-side fetch |
| Bootstrap + Turbo shell | Browser / Client | Frontend Server (Razor Pages render) | Shell HTML is server-rendered; Turbo Drive handles SPA-like navigation client-side |
| Sidebar toggle state | Browser / Client | — | CSS class toggle on `<body>`; `data-turbo-permanent` preserves DOM node across navigations |

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Razor Pages | 10.0 (in-box) | Page-per-route server-rendered UI, model binding, antiforgery | Constraint-mandated; page-centric model matches admin tool structure |
| `Microsoft.Extensions.Http` | in-box with .NET 10 | `IHttpClientFactory`, typed clients, handler pipeline | Official Microsoft pattern; prevents socket exhaustion; enables handler chaining |
| `System.Text.Json` | in-box | JSON deserialization of Backlot API `Envelope<T>` responses | In-box, zero extra deps; API returns plain JSON |
| ASP.NET Core Session | in-box | Server-side credential vault (`AddDistributedMemoryCache` + `AddSession`) | Session data never leaves server; aligns with auth security model |
| ASP.NET Core Cookie Auth | in-box | Identity gating: `[Authorize]`, `SignInAsync`, `SignOutAsync`, `LoginPath` redirect | Provides `[Authorize]` attribute, automatic login redirect, sign-out for free |
| `IHttpContextAccessor` | in-box | Access `HttpContext` (and `ISession`) from inside `DelegatingHandler` | Only way to read session from a handler; registered via `AddHttpContextAccessor()` |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `@hotwired/turbo` | 8.0.23 (CDN) | Turbo Drive (SPA-like navigation), `data-turbo-permanent` | All pages; loaded via CDN `<script>` in `_Layout.cshtml` |
| Bootstrap CSS | 5.3.8 (CDN) | Two-panel layout, login card, sidebar, utility classes | All pages |
| Bootstrap Icons | 1.13.1 (CDN CSS) | Sidebar nav icons, toggle hamburger/arrow | Sidebar only |
| `dotnet user-secrets` | .NET 10 CLI | Store `BacklotApi:BaseUrl` locally outside source | Dev environment only |

### Alternatives Considered
| Recommended | Alternative | Tradeoff |
|-------------|-------------|----------|
| `IHttpContextAccessor` in handler | Passing `ISession` directly | `ISession` is scoped; DelegatingHandlers live in a separate factory-managed scope — direct injection fails at runtime |
| Cookie auth + separate session | Storing creds in cookie | Cookie would expose Base64 credentials to the browser; violates the stated security boundary |
| `data-turbo="false"` on login form | Let Turbo handle login POST | Turbo Drive intercepts POST + redirect flows; auth form with cookie set-up needs standard browser round-trip to avoid race conditions |

**Installation:**
```bash
# No NuGet packages required beyond in-box ASP.NET Core shared framework
dotnet new webapp -n Backlot.Studio --framework net10.0
# Add to solution:
dotnet sln ../Backlot.sln add Backlot.Studio.csproj
```
CDN script/link tags handle Turbo, Bootstrap, Bootstrap Icons — no npm install.

---

## Package Legitimacy Audit

This phase installs zero external NuGet packages (all required functionality ships in the `Microsoft.AspNetCore.App` and `Microsoft.NETCore.App` shared frameworks bundled with the .NET 10 SDK). CDN assets (Turbo, Bootstrap, Bootstrap Icons) are linked via `<script>`/`<link>` tags — no package manager install.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| `Microsoft.AspNetCore.App` (shared framework) | .NET SDK (in-box) | ~10 yrs | — | github.com/dotnet/aspnetcore | OK | Approved — ships with .NET 10 SDK |
| `@hotwired/turbo@8.0.23` | CDN / npm | 4+ yrs | ~400K/wk | github.com/hotwired/turbo | OK [ASSUMED] | CDN link — no install |
| `bootstrap@5.3.8` | CDN / npm | 14+ yrs | ~5M/wk | github.com/twbs/bootstrap | OK [ASSUMED] | CDN link — no install |
| `bootstrap-icons@1.13.1` | CDN / npm | 4+ yrs | ~1M/wk | github.com/twbs/icons | OK [ASSUMED] | CDN link — no install |

**Packages removed due to SLOP verdict:** none

**Packages flagged as suspicious:** none

*CDN assets are pinned by version in the link/script tags. SRI integrity hashes (`integrity="sha384-..."`) are RECOMMENDED for all three CDN assets — executor should fetch canonical hashes from jsDelivr at implementation time.*

---

## Architecture Patterns

### System Architecture Diagram

```
Browser
  │ GET /page  (with .AspNetCore.Auth cookie)
  ▼
ASP.NET Core Middleware Pipeline
  ├── UseHttpsRedirection
  ├── UseStaticFiles
  ├── UseRouting
  ├── UseAuthentication      ← validates .AspNetCore.Auth cookie
  ├── UseAuthorization       ← [Authorize] sends unauthenticated → /login
  ├── UseSession             ← loads session (contains BasicAuth header string)
  └── MapRazorPages
        │
        ▼
  Razor Page OnGetAsync / OnPostAsync
        │  reads ViewData["Username"] from whoami
        │  calls → IBacklotApiService
        │               │
        │               ▼
        │         IHttpClientFactory
        │               │
        │               ▼
        │         BasicAuthHandler.SendAsync()
        │               │ reads HttpContext.Session["BasicAuthHeader"]
        │               │ injects Authorization: Basic <b64> header
        │               ▼
        │         Backlot API (https://localhost:7221)
        │               │ returns Envelope<T> JSON  OR  401
        │               ▼
        │         [if 401] → throw BacklotApiUnauthorizedException
        │
        ▼
  PageModel catches BacklotApiUnauthorizedException
        │ Response.Headers["Turbo-Visit-Control"] = "reload"
        │ Response.Redirect("/login")
        ▼
Browser performs full-page navigation to /login
```

### Recommended Project Structure
```
Backlot.Studio/
├── Pages/
│   ├── Shared/
│   │   ├── _Layout.cshtml          # Bootstrap shell, CDN tags, sidebar include
│   │   ├── _LoginLayout.cshtml     # Minimal layout for login (no sidebar)
│   │   └── _Sidebar.cshtml         # Sidebar partial (data-turbo-permanent)
│   ├── Login.cshtml                # Centered card, data-turbo="false" form
│   ├── Login.cshtml.cs
│   ├── Logout.cshtml               # POST handler only
│   ├── Logout.cshtml.cs
│   └── Index.cshtml                # Authenticated shell root (placeholder)
├── Services/
│   ├── IBacklotApiClient.cs        # Typed HttpClient interface
│   ├── BacklotApiClient.cs         # Implementation; calls Backlot API
│   ├── BasicAuthHandler.cs         # DelegatingHandler; reads ISession
│   ├── ApiEnvelope.cs              # Envelope<T> deserialization model
│   └── BacklotApiUnauthorizedException.cs
├── wwwroot/
│   └── openapidoc.json             # Served statically (used in Phase 2)
├── appsettings.json                # BacklotApi:BaseUrl = https://localhost:7221
├── Program.cs                      # DI wiring, middleware order
└── Backlot.Studio.csproj
```

### Pattern 1: BasicAuthHandler — Reading Session Per Request
**What:** A `DelegatingHandler` that reads the Basic Auth header string from `ISession` on every outbound call and injects it into the `Authorization` header.
**When to use:** Every outbound `HttpClient` call to the Backlot API.

```csharp
// Source: Microsoft Learn — Use HttpContext in ASP.NET Core (aspnetcore-10.0)
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context
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
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthHeader);
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

Registration in `Program.cs`:
```csharp
// Source: Microsoft Learn — Make HTTP requests using IHttpClientFactory in ASP.NET Core
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BasicAuthHandler>();

builder.Services.AddHttpClient<IBacklotApiClient, BacklotApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BacklotApi:BaseUrl"]
        ?? "https://localhost:7221");
}).AddHttpMessageHandler<BasicAuthHandler>();
```

### Pattern 2: Session Configuration (8-hour Sliding Timeout)
**What:** `ISession` as the secure server-side credential vault.
**When to use:** `Program.cs` setup.

```csharp
// Source: Microsoft Learn — Session in ASP.NET Core (aspnetcore-10.0)
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);   // D-04/D-05: 8-hour sliding
    options.Cookie.HttpOnly = true;                // Not accessible to JS
    options.Cookie.IsEssential = true;             // No consent banner needed
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF hardening
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
});
```

Middleware order (CRITICAL — UseSession must come after UseRouting):
```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();          // After UseRouting, before MapRazorPages
app.MapRazorPages();
```

### Pattern 3: Cookie Auth Setup (UI Gating)
**What:** Lightweight cookie scheme that drives `[Authorize]` redirects. Separate from API credentials.
**When to use:** `Program.cs` setup.

```csharp
// Source: Microsoft Learn — Cookie authentication without ASP.NET Core Identity (aspnetcore-10.0)
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;            // D-05
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
```

Login handler in `Login.cshtml.cs`:
```csharp
// Source: Microsoft Learn — Cookie authentication without ASP.NET Core Identity (aspnetcore-10.0)
public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid) return Page();

    // 1. Build Basic Auth header value
    var raw = $"{Input.Username}:{Input.Password}";
    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    var headerValue = encoded; // stored without "Basic " prefix

    // 2. Validate against API
    HttpContext.Session.SetString("BasicAuthHeader", headerValue);
    var isValid = await _apiClient.IsAuthenticatedAsync();
    if (!isValid)
    {
        HttpContext.Session.Remove("BasicAuthHeader");
        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        return Page();
    }

    // 3. Sign in cookie auth
    var claims = new List<Claim> { new Claim(ClaimTypes.Name, Input.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return LocalRedirect(ReturnUrl ?? "/");
}
```

Logout handler in `Logout.cshtml.cs`:
```csharp
public async Task<IActionResult> OnPostAsync()
{
    HttpContext.Session.Clear();
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToPage("/login");
}
```

### Pattern 4: 401 → Top-Level Redirect (Turbo-Safe)
**What:** When `BasicAuthHandler` detects a 401, the page must redirect to `/login` as a full-page navigation, not as a Turbo Frame response.
**When to use:** Middleware or a base `PageModel` method that catches `BacklotApiUnauthorizedException`.

Option A — Base PageModel pattern (recommended for Phase 1):
```csharp
// Source: Turbo Handbook — Drive section (turbo.hotwired.dev/handbook/frames)
// target="_top" / turbo-visit-control causes full-page navigation
public abstract class AuthenticatedPageModel : PageModel
{
    protected async Task<T?> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try
        {
            return await apiCall();
        }
        catch (BacklotApiUnauthorizedException)
        {
            // Signal Turbo to perform a full-page visit, not a frame update
            Response.Headers["Turbo-Visit-Control"] = "reload";
            Response.Redirect("/login");
            return default;
        }
    }
}
```

Option B — Razor response meta tag (simpler but per-page):
```html
<!-- In the Razor page or layout when redirecting: -->
<meta name="turbo-visit-control" content="reload">
```

The `Turbo-Visit-Control: reload` HTTP response header is equivalent and more reliable from server-side code. [CITED: turbo.hotwired.dev/handbook/frames — "Pages that specify turbo-visit-control reload will always result in a full-page navigation, even if the request originated inside a frame."]

### Pattern 5: Sidebar with `data-turbo-permanent`
**What:** The sidebar element is marked `data-turbo-permanent` so Turbo Drive preserves its DOM node (and CSS classes, including collapse state) across page navigations.
**When to use:** `_Sidebar.cshtml` partial and `_Layout.cshtml`.

```html
<!-- Source: Turbo Handbook — Drive section (turbo.hotwired.dev/handbook/drive) -->
<aside id="sidebar" data-turbo-permanent>
  <!-- Sidebar content: app name, nav, identity block, toggle button -->
</aside>
```

Important: `data-turbo-permanent` requires the element to have a stable `id` attribute. Turbo matches elements across page loads by `id`. Without `id`, permanent preservation does not work.

Login form opt-out of Turbo:
```html
<!-- data-turbo="false" prevents Turbo Drive from intercepting the POST -->
<form method="post" asp-page="/login" data-turbo="false">
    @Html.AntiForgeryToken()
    <!-- inputs -->
</form>
```

### Anti-Patterns to Avoid
- **Injecting `ISession` directly into `DelegatingHandler`:** `ISession` is a scoped service; `DelegatingHandler` instances live in the factory's long-lived handler pool with a separate DI scope. Direct injection causes `ObjectDisposedException` in production. Use `IHttpContextAccessor` instead and read session inside `SendAsync`.
- **`new HttpClient()` per request:** Socket exhaustion and DNS staleness. Always use `IHttpClientFactory` via `AddHttpClient<T>()`.
- **Storing Basic Auth credentials in a cookie:** Credentials are base64-reversible. If the auth cookie contains the credentials, they are exposed to the browser. Store credentials in server-side `ISession` only.
- **Calling `Response.Redirect("/login")` without `Turbo-Visit-Control: reload`:** Inside a Turbo Frame or after a frame-targeted request, a bare redirect is rendered inside the frame — the user sees the login page injected into a sidebar or partial area rather than a full-page replacement.
- **`UseSession()` before `UseRouting()`:** Session middleware must come after routing so it can participate in endpoint dispatch. Reversing the order causes session to not load for requests that are still being matched.
- **Calling `SignInAsync` before verifying credentials against the API:** If the sign-in fires before the API check, the cookie is issued for invalid credentials. Validate first, sign in second.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Auth cookie creation/decryption | Custom encryption/signing | ASP.NET Core Data Protection (in `AddCookie`) | Data Protection handles key rotation, encryption algorithm, and tamper detection |
| Anti-CSRF tokens | Custom header/token scheme | `asp-page` form tag + default antiforgery middleware | Built-in; already wired by Razor Pages; hand-rolling re-introduces known CSRF vectors |
| Session ID generation | Custom session ID | `AddSession` built-in | Cryptographically random IDs, tied to `IDistributedCache` backend |
| Pooled HTTP connection management | `new HttpClient()` | `IHttpClientFactory` | Factory manages handler lifetimes and socket recycling |
| JSON deserialization | Manual string parsing | `System.Text.Json.JsonSerializer.DeserializeAsync<T>` | Handles encoding, null safety, streaming; hand-rolling introduces injection surfaces |

**Key insight:** All five of these domains have subtle security or reliability edge cases that the ASP.NET Core team has already solved. The framework's built-in solutions pass security audits; hand-rolled replacements typically do not.

---

## Common Pitfalls

### Pitfall 1: DelegatingHandler Scope Confusion
**What goes wrong:** `ISession` or other scoped services injected into a `DelegatingHandler` constructor throw `ObjectDisposedException` in production under load.
**Why it happens:** `IHttpClientFactory` manages `HttpMessageHandler` instances in a pool that outlives request scopes. When a handler is created, the DI scope from registration time is used — not the current request's scope. By the time `SendAsync` is called, the injected scoped service's scope may already be disposed.
**How to avoid:** Inject `IHttpContextAccessor` (singleton-safe) and access `HttpContext.Session` inside `SendAsync` (not the constructor). Register the handler as `Transient`.
**Warning signs:** `ObjectDisposedException: 'ISession' has been disposed` in logs, typically after several minutes of operation.

### Pitfall 2: 401 Redirect Captured Inside Turbo Frame
**What goes wrong:** User gets mid-session 401; login form appears inside a Turbo Frame region (e.g., a partial nav area) instead of as a full-page redirect.
**Why it happens:** Turbo Drive intercepts the redirect response and renders it inside the currently active frame target. Without `Turbo-Visit-Control: reload`, Turbo assumes frame-scoped rendering.
**How to avoid:** Set `Response.Headers["Turbo-Visit-Control"] = "reload"` before any redirect that should force full-page navigation. Test explicitly by triggering a 401 while a frame request is in flight.
**Warning signs:** Login page HTML appearing inside a `<div>` or partial area on an authenticated page.

### Pitfall 3: `UseSession()` Middleware Order
**What goes wrong:** Session is always empty; `HttpContext.Session.GetString("BasicAuthHeader")` always returns null.
**Why it happens:** `UseSession()` must be called after `UseRouting()` and after `UseAuthentication()`/`UseAuthorization()`, but before `MapRazorPages()`. If called too early or too late, the session middleware either doesn't activate or can't write session state because the response has already started.
**How to avoid:** Follow the documented middleware order: `UseRouting → UseAuthentication → UseAuthorization → UseSession → MapRazorPages`.
**Warning signs:** Session reads return null even immediately after `SetString` in the same request.

### Pitfall 4: `data-turbo-permanent` Without Stable `id`
**What goes wrong:** Sidebar collapse state is lost on every Turbo navigation; sidebar reverts to expanded on each page load.
**Why it happens:** Turbo Drive uses the `id` attribute to match permanent elements across page loads. Without a matching `id`, Turbo replaces the element rather than preserving it.
**How to avoid:** Set `id="sidebar"` (or another stable string) on the `<aside>` element alongside `data-turbo-permanent`.
**Warning signs:** Sidebar state resets on navigation; toggle icon reverts to hamburger after clicking a link.

### Pitfall 5: Login Form Intercepted by Turbo Drive
**What goes wrong:** After login POST, the auth cookie is set, but Turbo replaces the page body without triggering a full browser cookie update — next navigation appears unauthenticated.
**Why it happens:** Turbo Drive intercepts the form POST and processes the redirect response via `fetch`, not a full browser navigation. Cookie-setting `Set-Cookie` headers from a `fetch`-driven redirect are applied, but the subsequent Turbo page swap may not re-evaluate cookie state correctly.
**How to avoid:** Add `data-turbo="false"` to the login form. This ensures a standard browser form POST → Set-Cookie → redirect cycle.
**Warning signs:** User appears logged in (cookie set) but `[Authorize]` still redirects them, or session state is missing on the page after redirect.

### Pitfall 6: Cookie Auth Expiry vs Session Expiry Mismatch
**What goes wrong:** User's cookie expires before their session (or vice versa), creating confusing states where the API rejects calls but the cookie auth still considers them authenticated, or the session is gone but the cookie is still valid.
**Why it happens:** Two separate expiry timers: `CookieAuthenticationOptions.ExpireTimeSpan` controls the auth cookie; `SessionOptions.IdleTimeout` controls the session. Default values differ (cookie default 14 days sliding; session default 20 minutes).
**How to avoid:** Set both to 8 hours with `SlidingExpiration = true`. Explicitly set both in `Program.cs`. The session expiry governs when API credentials are lost; the cookie expiry governs when `[Authorize]` starts redirecting.
**Warning signs:** Users report being "logged out" (auth cookie redirect) but session still shows credentials, or the opposite.

---

## Code Examples

### Envelope Deserialization
```csharp
// Backlot API wraps every response in { "Body": T, "Status": string, "TimeInMs": int }
// Source: openapidoc.json — all endpoints share this response schema
public record ApiEnvelope<T>
{
    public T? Body { get; init; }
    public string? Status { get; init; }
    public long TimeInMs { get; init; }
}

// In BacklotApiClient:
private async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
{
    var response = await _httpClient.GetAsync(path, ct);
    response.EnsureSuccessStatusCode();
    var envelope = await response.Content
        .ReadFromJsonAsync<ApiEnvelope<T>>(cancellationToken: ct);
    return envelope?.Body;
}
```

### Whoami — Server-Side Identity Call
```csharp
// Source: openapidoc.json — GET /api/role/director/whoami returns { "Body": object }
// Called from authenticated PageModel base class; result passed via ViewData
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

### SRI Integrity Hash Fetch (Implementation Note)
```
Executor must fetch SRI hashes for CDN assets from jsDelivr at implementation time:
https://www.jsdelivr.com/package/npm/@hotwired/turbo?version=8.0.23
https://www.jsdelivr.com/package/npm/bootstrap?version=5.3.8
https://www.jsdelivr.com/package/npm/bootstrap-icons?version=1.13.1

Include as integrity="sha384-..." on all CDN <link> and <script> tags.
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `services.AddAuthentication()` in `Startup.cs` | `builder.Services.AddAuthentication()` in `Program.cs` | .NET 6 | No `Startup.cs` — everything in `Program.cs` with minimal hosting model |
| `ISession` — synchronous reads | `await session.LoadAsync()` before read | ASP.NET Core 6+ | Async load avoids synchronous blocking; call `LoadAsync` before `TryGetValue` in handlers |
| Cookie auth `options.LoginPath` fallback | ASP.NET Core 10: known API endpoints no longer redirect — return 401/403 directly | ASP.NET Core 10 | For Razor Pages (non-API endpoints), redirect still applies; relevant distinction if Studio ever adds API endpoints alongside pages |
| Turbo 7 `data-turbo-permanent` | Turbo 8 `data-turbo-permanent` | Turbo 8.x | Same attribute; Turbo 8 adds morphing and refresh improvements — behaviour of permanent elements unchanged |

**Deprecated/outdated:**
- `Startup.cs` + `ConfigureServices`/`Configure`: Replaced by `Program.cs` minimal hosting model in .NET 6+. Do not create a `Startup.cs` for this project.
- `services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()`: Current idiom is `builder.Services.AddHttpContextAccessor()` (extension method, registered since .NET 6).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `whoami` endpoint returns an object whose `Body` contains a field the planner can use as a username string | Code Examples (WhoAmI) | If `Body` is typed differently (e.g. nested object), the `ToString()` fallback returns the type name, not the username; requires inspecting actual API response shape at runtime |
| A2 | The Backlot API returns HTTP 401 (not 403 or a 200 with an error body) when Basic Auth credentials are wrong | Architecture Patterns (BasicAuthHandler) | If the API returns 200 with a non-success `Status` field, the 401 detection logic in `BasicAuthHandler` never fires and credentials are silently accepted |
| A3 | CDN versions for @hotwired/turbo 8.0.23, bootstrap 5.3.8, bootstrap-icons 1.13.1 are available and legitimate | Package Legitimacy Audit | Low risk — all three are major, well-established projects; verified via CLAUDE.md pinned versions |
| A4 | `Turbo-Visit-Control: reload` response header causes Turbo 8 to perform a full-page visit | Common Pitfalls (Pitfall 2), Code Examples | The Turbo handbook confirms the `<meta>` tag approach; the HTTP header equivalent is documented in Turbo source but not the handbook page fetched. If the header is ignored, fall back to setting the meta tag in the redirect response |

---

## Open Questions

1. **What does the `whoami` response body contain?**
   - What we know: `GET /api/role/director/whoami` returns `{ "Body": object }` per openapidoc.json — `Body` is typed as plain `object` in the spec.
   - What's unclear: The actual shape — is it a string (username), a dict, a serialized role? The `ToString()` fallback is safe but may display the wrong text.
   - Recommendation: During Plan 01-03 implementation, call the endpoint against a running API and inspect the actual `Body` value; adjust the `WhoAmIAsync` return type accordingly.

2. **`Turbo-Visit-Control` header vs meta tag for 401 redirects**
   - What we know: The Turbo handbook documents the `<meta name="turbo-visit-control" content="reload">` tag. The HTTP header form (`Turbo-Visit-Control: reload`) is referenced in Turbo source and community usage.
   - What's unclear: Whether Turbo 8.0.23 specifically handles the HTTP response header form, or only the meta tag form.
   - Recommendation: Implement using the HTTP response header first; if integration testing shows it is ignored, add the meta tag to a dedicated redirect-to-login Razor page as fallback.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | Build, run | ✓ | 10.0.109 | — |
| Node.js | Development tooling only (not required for build) | ✓ | 24.14.0 | — |
| Backlot API (localhost:7221) | Runtime integration testing | Not verified | — | Use mock `IBacklotApiClient` in unit tests; integration tests require running API |
| jsDelivr CDN | CDN asset delivery (Turbo, Bootstrap) | ✓ (network) | — | LibMan to vendor assets locally if offline |

**Missing dependencies with no fallback:** The Backlot API must be running at the configured `BacklotApi:BaseUrl` for end-to-end manual verification. Local development works with `dotnet run --project Backlot.Demo.Web` as the backing API.

---

## Project Constraints (from CLAUDE.md)

The following directives from `CLAUDE.md` (both project-level and Studio-specific) apply directly to Phase 1 planning:

| Source | Directive | Enforcement |
|--------|-----------|-------------|
| Studio CLAUDE.md | No React/Vue/Angular or npm build pipeline | CDN-only for Turbo/Bootstrap/Bootstrap Icons |
| Studio CLAUDE.md | Typed `HttpClient` via `IHttpClientFactory` only; never `new HttpClient()` | Enforced in `BasicAuthHandler` and service layer |
| Studio CLAUDE.md | Auth credentials never stored in browser (localStorage/JS cookie) | `ISession` server-side only; cookie auth holds only identity signal |
| Studio CLAUDE.md | Use `AddSession` + `AddDistributedMemoryCache` + `AddAuthentication().AddCookie()` | Pattern confirmed in session and cookie auth sections |
| Studio CLAUDE.md | Turbo 8.0.23 (CDN pinned); no Turbo 7.x | Pinned in CDN script tags |
| Studio CLAUDE.md | Bootstrap 5.3.8 (CDN pinned) | Pinned in CDN link tag |
| Studio CLAUDE.md | Bootstrap Icons 1.13.1 (CDN CSS pinned) | Pinned in CDN link tag |
| Studio CLAUDE.md | Always HTTPS to the API; HttpOnly session cookie | `SecurePolicy = Always` in session and cookie auth options |
| Backlot CLAUDE.md | .NET 10 target framework | `<TargetFramework>net10.0</TargetFramework>` in `.csproj` |
| GSD config | `nyquist_validation: false` | No Validation Architecture section in this research |
| GSD config | `security_enforcement: true`, `security_asvs_level: 1` | Security Domain section included below |

---

## Security Domain

ASVS level 1 is configured (`security_asvs_level: 1`). Level 1 covers opportunistic security — preventing the most common vulnerabilities.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | Yes | Cookie auth + API credential validation via `isauthenticated` endpoint |
| V3 Session Management | Yes | `AddSession` with HttpOnly/Secure/SameSite/8h sliding timeout |
| V4 Access Control | Yes | `[Authorize]` on all authenticated pages; `LoginPath` redirect |
| V5 Input Validation | Yes | Model binding DataAnnotations on `LoginInputModel` (Username, Password required); no raw SQL |
| V6 Cryptography | Partial | ASP.NET Core Data Protection encrypts auth cookie; session cookie is an opaque ID — no credential material in cookie |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Credential theft via browser JS | Information Disclosure | Credentials stored server-side in `ISession`; session cookie is `HttpOnly` — inaccessible to JS |
| CSRF on login/logout form | Tampering | Razor Pages antiforgery tokens (`asp-page` form tag + built-in middleware) |
| Session fixation | Elevation of Privilege | `SignInAsync` issues a new auth cookie; `HttpContext.Session.Clear()` on logout destroys session data |
| 401 credential replay inside Turbo Frame | Spoofing | `BasicAuthHandler` throws typed exception; caught at page level; full-page redirect prevents frame-scoped exposure |
| Credentials in cookies | Information Disclosure | Only opaque session ID in session cookie; only ClaimsPrincipal (username) in auth cookie — not password |
| Mixed HTTP/HTTPS credential transit | Information Disclosure | `SecurePolicy = CookieSecurePolicy.Always` rejects non-HTTPS; `UseHttpsRedirection()` enforced |

---

## Sources

### Primary (MEDIUM confidence — webfetch from official Microsoft Learn)
- [Microsoft Learn — Make HTTP requests using IHttpClientFactory (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0) — typed client registration, DelegatingHandler pattern, `IHttpContextAccessor` usage in handlers, handler chaining
- [Microsoft Learn — Session in ASP.NET Core (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0) — `AddDistributedMemoryCache`, `AddSession`, `SessionOptions`, middleware order, `IdleTimeout`
- [Microsoft Learn — Cookie authentication without ASP.NET Core Identity (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) — `AddAuthentication().AddCookie()`, `SignInAsync`, `SignOutAsync`, `SlidingExpiration`, ASP.NET Core 10 API endpoint behavior change
- [Microsoft Learn — Use HttpContext in ASP.NET Core (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0) — `IHttpContextAccessor` thread-safety warnings, `HttpContext` in handlers, `UserAgentHeaderHandler` pattern

### Secondary (LOW confidence — webfetch from Turbo handbook)
- [Turbo Handbook — Drive](https://turbo.hotwired.dev/handbook/drive) — `data-turbo="false"` opt-out, HTTP redirect handling
- [Turbo Handbook — Frames](https://turbo.hotwired.dev/handbook/frames) — `turbo-visit-control` for full-page navigation from frame context, `target="_top"` frame navigation

### Tertiary (in-project — HIGH confidence)
- `openapidoc.json` — Backlot API endpoint schemas; `whoami` returns `Body: object`; `isauthenticated` returns `Body: boolean`
- `Backlot.Studio/.claude/CLAUDE.md` — pinned stack versions, architecture-critical patterns, "What NOT to Use" list
- `Backlot.Studio/.planning/phases/01-foundation-auth/01-CONTEXT.md` — locked decisions D-01 through D-09
- `Backlot.Studio/.planning/phases/01-foundation-auth/01-UI-SPEC.md` — component inventory, interaction contract, copywriting contract

---

## Metadata

**Confidence breakdown:**
- Standard stack: MEDIUM — all libraries are in-box .NET 10; CDN assets from official sources; CLAUDE.md pins are authoritative
- Architecture: MEDIUM — DelegatingHandler + IHttpContextAccessor pattern confirmed from official Microsoft Learn docs; Turbo permanent element pattern confirmed from handbook
- Pitfalls: MEDIUM — scope confusion and 401-in-frame pitfalls are well-documented in Microsoft Learn and Turbo handbook respectively; session middleware order is documented
- Security: MEDIUM — ASVS level 1 controls mapped to in-box ASP.NET Core features; all controls are framework-standard

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (stable — .NET 10 LTS and Turbo 8 are not rapidly-changing)
