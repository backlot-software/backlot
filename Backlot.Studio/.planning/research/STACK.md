# Stack Research

**Domain:** .NET Razor Pages admin/management frontend (server-rendered) proxying a REST API, with Hotwired Turbo + Bootstrap
**Researched:** 2026-06-21
**Confidence:** HIGH (versions verified against npm registry + Microsoft docs; integration patterns MEDIUM)

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET / ASP.NET Core | **10.0** (LTS) | Runtime + web host for Razor Pages | .NET 10 went GA Nov 2025 and is LTS through Nov 2028. Matches the Backlot framework target (.NET 10 per CLAUDE.md). No reason to pin to 8 — same project already builds on 10. |
| Razor Pages | (ASP.NET Core 10) | Page-per-route server-rendered UI | Page-centric model fits an admin tool's "one screen = one page" structure better than MVC controllers or Minimal APIs. Built-in model binding + antiforgery + `PageModel` handlers map cleanly to the proxy-and-render flow. |
| Hotwired Turbo (`@hotwired/turbo`) | **8.0.23** | SPA-like navigation (Turbo Drive), partial updates (Turbo Frames), targeted DOM mutation (Turbo Streams) | Constraint-mandated. Turbo 8 is the current major (8.x). Delivers SPA feel with zero JS build pipeline — exactly the project's stated goal ("SPA-like UX without a JS framework build pipeline"). Drive intercepts links/forms; Frames let the Scalar side panel and role-detail sections load independently. |
| Bootstrap | **5.3.8** | Layout, components, utility classes, dark mode | Constraint-mandated. 5.3.x is the current line and includes the built-in color-mode (dark/light) system — useful for a developer-facing tool. CDN-deliverable, no build step. |
| Scalar API Reference (`@scalar/api-reference`) | **1.60.0** | Embedded OpenAPI reference side panel | Constraint-mandated. Renders `openapidoc.json` directly. Loaded via single CDN script + `Scalar.createApiReference()` — no build integration, matching the "zero build-step" decision in PROJECT.md. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Icons | **1.13.1** | Icon set | Sidebar nav, action buttons, relation indicators. CDN CSS, no build. Pairs with Bootstrap. Optional but expected for an admin UI. |
| `Microsoft.Extensions.Http` | (in-box with .NET 10 SDK) | `IHttpClientFactory` + typed clients | Always — this is how Studio talks to the Backlot API. Provides pooled handlers and a clean place to attach the auth `DelegatingHandler`. |
| `System.Text.Json` | (in-box) | (De)serialize Backlot API JSON | Always — default serializer. No need for Newtonsoft. The API returns plain JSON (`Body`, `Status`, role objects). |
| ASP.NET Core Session (`AddSession` + `AddDistributedMemoryCache`) | (in-box) | Server-side credential storage | Always — stores the base64 `username:password` server-side so it never reaches the browser (the stated security boundary). |
| ASP.NET Core Cookie Authentication (`AddAuthentication().AddCookie()`) | (in-box) | Auth/redirect gating | Recommended — use a lightweight cookie scheme to drive `[Authorize]` redirects to `/login`, while the *API* credentials live in session. See auth note below. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `dotnet` CLI (.NET 10 SDK) | Build/run/test | `dotnet new razor`, `dotnet run`, add to `Backlot.sln`. |
| LibMan (optional) | Pin CDN assets locally | Only if you want offline/self-hosted Turbo/Bootstrap/Scalar instead of CDN. Keeps "no npm build" promise. Skip for v1 (CDN is simpler). |
| `dotnet user-secrets` | Local config (API base URL) | Store `BacklotApi:BaseUrl` outside source. Defaults to `https://localhost:7221`. |

## Installation

This is a .NET project — front-end assets come via CDN, not npm. There is **no `package.json` and no JS build step** (intentional, per constraints).

```bash
# Scaffold the project and add to the solution
dotnet new razor -n Backlot.Studio -o Backlot.Studio --framework net10.0
dotnet sln Backlot.sln add Backlot.Studio/Backlot.Studio.csproj

# No extra NuGet packages strictly required for v1:
#   IHttpClientFactory, Session, Cookie auth, System.Text.Json are all in the
#   Microsoft.AspNetCore.App / Microsoft.NETCore.App shared frameworks.
# Add ONLY if you opt into resilience/polly later:
# dotnet add package Microsoft.Extensions.Http.Resilience
```

CDN assets (in `_Layout.cshtml`):

```html
<!-- Bootstrap 5.3.8 CSS -->
<link rel="stylesheet"
      href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css"
      integrity="..." crossorigin="anonymous">

<!-- Bootstrap Icons 1.13.1 -->
<link rel="stylesheet"
      href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">

<!-- Bootstrap JS bundle (Popper included) -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js"
        integrity="..." crossorigin="anonymous"></script>

<!-- Hotwired Turbo 8.0.23 (ESM module) -->
<script type="module"
        src="https://cdn.jsdelivr.net/npm/@hotwired/turbo@8.0.23/dist/turbo.es2017-esm.min.js"></script>

<!-- Scalar API Reference 1.60.0 (load only on the page hosting the side panel) -->
<script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.60.0"></script>
```

> Pin exact versions in CDN URLs (not `@latest`) for reproducibility and to avoid a Turbo/Scalar upgrade silently breaking the UI. Add SRI `integrity` hashes for Bootstrap (jsdelivr provides them).

## Architecture-Critical Patterns

### 1. API proxying via typed `HttpClient` + auth `DelegatingHandler`

The browser must never see the API credentials. Store base64 `username:password` in **server-side session** at login, then inject it on every outbound API call via a `DelegatingHandler` that reads `IHttpContextAccessor` → `Session`.

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });

builder.Services.AddTransient<BasicAuthHandler>();
builder.Services.AddHttpClient<BacklotApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["BacklotApi:BaseUrl"]!))
    .AddHttpMessageHandler<BasicAuthHandler>();
```

`BasicAuthHandler` adds `Authorization: Basic <base64>` from session to each request. This keeps auth concerns out of every page handler.

### 2. Turbo + Razor Pages handler convention

Turbo Drive works out of the box once the script loads (it hijacks `<a>`/`<form>`). For partial updates:
- Wrap the Scalar side panel and role-detail sections in `<turbo-frame id="...">`.
- A Razor Page handler can return only the frame's HTML (a partial) for frame requests, detected via the `Turbo-Frame` request header.
- Use `data-turbo-frame` / `data-turbo="false"` attributes to opt specific links in/out of frame targeting.

### 3. Scalar side panel init

Modern Scalar (1.x) uses the JS API, not the old `data-url` script attribute:

```html
<div id="scalar-panel"></div>
<script>
  Scalar.createApiReference('#scalar-panel', {
    url: '/openapidoc.json',           // serve the spec from Studio (or proxy it)
    // optional: filter/scroll to a specific operation for the clicked scenario
  });
</script>
```

Serve `openapidoc.json` from Studio (static file or a proxy endpoint) so Scalar isn't blocked by CORS against the API host.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Razor Pages | ASP.NET Core MVC | If you needed many shared controllers/complex routing. Overkill here — Studio is page-centric. |
| Razor Pages + Turbo | Blazor Server / Blazor Web App | If the team wanted C#-only interactivity. Rejected: heavier, websocket/circuit complexity, and contradicts the explicit "no SPA framework, use Turbo" constraint. |
| Typed `HttpClient` via factory | `new HttpClient()` per request | Never — socket exhaustion + no handler pipeline for auth. Always use the factory. |
| Cookie auth for gating + session for API creds | Storing creds only in session, gate manually | Fine for v1, but cookie auth gives you `[Authorize]`, `LoginPath` redirects, and sign-out for free. Recommended. |
| CDN assets | LibMan / self-hosted | Use LibMan only if offline builds or strict CSP without external origins are required. |
| `System.Text.Json` | Newtonsoft.Json | Only if the API emitted polymorphic/edge-case JSON STJ can't handle. Not the case here. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| React / Vue / Angular / any SPA | Explicitly out of constraints; adds a Node build pipeline the project wants to avoid | Razor Pages + Turbo |
| npm / webpack / Vite build for front-end | Defeats the "zero build-step" decisions in PROJECT.md | CDN script/link tags (pin versions) |
| Storing API credentials in browser (localStorage/JS cookie) | Breaks the security boundary — creds would be exposed to client JS | Server-side `ISession`, proxied requests |
| Turbo 7.x | Superseded; 8.x is current with refresh/morphing improvements | `@hotwired/turbo@8.0.23` |
| Bootstrap 4 / older 5.0–5.2 | No built-in color-mode (dark/light); missing utilities | Bootstrap `5.3.8` |
| Scalar `data-url` script-attribute embed | Older init style; current API is `Scalar.createApiReference()` | `@scalar/api-reference@1.60.0` + `createApiReference()` |
| `new HttpClient()` directly | Socket exhaustion, DNS-staleness, no auth handler pipeline | `AddHttpClient<T>()` typed client |
| Bare base64 "Basic" with no transport security | Credentials are reversible | Always HTTPS to the API; HttpOnly session cookie |

## Stack Patterns by Variant

**If you must support strict CSP / offline (air-gapped) deployment:**
- Use LibMan to vendor Bootstrap, Turbo, and Scalar into `wwwroot/lib`.
- Add a per-request `nonce` to the Scalar inline init script (Scalar supports nonce).

**If the admin tool grows to multiple operators / horizontal scaling:**
- Replace `AddDistributedMemoryCache()` with a distributed cache (Redis via `AddStackExchangeRedisCache`) so session/auth survives across instances.

**If API calls need resilience (retries/timeouts):**
- Add `Microsoft.Extensions.Http.Resilience` and `.AddStandardResilienceHandler()` to the typed client. Defer for v1.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| .NET 10 SDK | ASP.NET Core 10, C# 14 | All in-box features (Session, IHttpClientFactory, cookie auth) ship in the shared framework. |
| `@hotwired/turbo@8.0.23` | Bootstrap 5.3.8 | No conflict — Turbo controls navigation, Bootstrap controls styling/components. Re-init Bootstrap JS components on `turbo:load` if you use dynamically-rendered modals/tooltips. |
| `@scalar/api-reference@1.60.0` | Turbo Frames | Render the Scalar mount inside a Turbo Frame; call `createApiReference()` after the frame loads (`turbo:frame-load`). |
| Bootstrap Icons 1.13.1 | Bootstrap 5.3.8 | Independent CSS font; any 1.x works. |

## Sources

- `https://registry.npmjs.org/@hotwired/turbo/latest` — Turbo 8.0.23 (npm registry, authoritative) — HIGH
- `https://registry.npmjs.org/bootstrap/latest` — Bootstrap 5.3.8 (npm registry, authoritative) — HIGH
- `https://registry.npmjs.org/@scalar/api-reference/latest` — Scalar 1.60.0 (npm registry, authoritative) — HIGH
- `https://registry.npmjs.org/bootstrap-icons/latest` — Bootstrap Icons 1.13.1 (npm registry, authoritative) — HIGH
- Microsoft .NET blog / Microsoft Learn — .NET 10 GA Nov 2025, LTS to Nov 2028; ASP.NET Core 10 — HIGH
- Microsoft Learn (IHttpClientFactory, DelegatingHandlers) — typed client + auth handler pattern — HIGH
- scalar.com docs / GitHub README — `createApiReference()` CDN embed, nonce/CSP — MEDIUM
- turbo.hotwired.dev handbook — Turbo Drive/Frames CDN install — MEDIUM
- Backlot `PROJECT.md` + `openapidoc.json` — constraints, auth model, API endpoints — HIGH

---
*Stack research for: .NET Razor Pages + Turbo + Bootstrap admin frontend*
*Researched: 2026-06-21*
