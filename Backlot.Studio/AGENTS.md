# AGENTS.MD

## Project Backlot.Studio

Backlot.Studio is a .NET Razor Class Library (RCL) that serves as the management frontend for the Backlot API. It gives developers and operators a visual interface to browse all registered scenarios, manage persisted roles (search, view details, edit), and inspect role relations — all backed by the Backlot API. It is embedded into a host app (e.g. the Backlot API web host) via `builder.Services.AddBacklotStudio(...)` + `app.MapBacklotStudio("/studio")`; the pages live in the `Studio` MVC area (`Areas/Studio/Pages`) and `wwwroot` is embedded and served from `{prefix}/assets`. See `README.md` for the mounting contract and `BacklotStudioExtensions.cs` for the implementation.

**Why an area, given there is only one?** The `Studio` area does not separate Studio from other areas in this project — it separates Studio's Razor Pages from *the host application's*. It is the selector for all three conventions in `BacklotStudioExtensions.cs` (`AddAreaFolderRouteModelConvention` applies the mount-path prefix, `AuthorizeAreaFolder`/`AllowAnonymousToAreaPage` scope the Studio cookie policy), and it keeps page paths like `Index`, `Login` and `Error` — `Index.cshtml` is `@page "/"` — out of a host's own `/Pages` root. Removing it would prefix and authorize the host's pages too. The area name never appears in a URL. Only `Areas/Studio/Pages` belongs under `Areas/`; plain C# (`Core/`, `Implementation/`, `Extensions/`) stays at the project root. `ViewModels/` lives under `Areas/Studio/Pages/` alongside the pages that consume them.

**Core Value:** A developer or operator can find any role in the system, inspect its state and relations, and edit it — without writing a single API call by hand.

### Constraints

- **Tech Stack**: .NET Razor Pages + TurboJS + Bootstrap — no React/Vue/SPA framework
- **Project**: Razor Class Library `.csproj` (`Microsoft.NET.Sdk.Razor`) inside `Backlot.Studio/`, packed as a NuGet package and mounted by a host. The project it self is not a standalone web host (no `Program.cs`). A host can run it on its own, against an API in another process, like demo'ed in `Backlot.Demo.Studio/`.
- **API**: All data comes from the Backlot API; Studio has no database of its own
- **Auth**: Basic Auth only — username/password encoded as base64, sent on every request

## Technology Stack

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET / ASP.NET Core | **10.0** (LTS) | Runtime + web host for Razor Pages | .NET 10 went GA Nov 2025 and is LTS through Nov 2028. Matches the Backlot framework target (.NET 10 per CLAUDE.md). No reason to pin to 8 — same project already builds on 10. |
| Razor Pages | (ASP.NET Core 10) | Page-per-route server-rendered UI | Page-centric model fits an admin tool's "one screen = one page" structure better than MVC controllers or Minimal APIs. Built-in model binding + antiforgery + `PageModel` handlers map cleanly to the proxy-and-render flow. |
| Hotwired Turbo (`@hotwired/turbo`) | **8.0.23** | SPA-like navigation (Turbo Drive), partial updates (Turbo Frames), targeted DOM mutation (Turbo Streams) | Constraint-mandated. Turbo 8 is the current major (8.x). Delivers SPA feel with zero JS build pipeline — exactly the project's stated goal ("SPA-like UX without a JS framework build pipeline"). Drive intercepts links/forms; Frames let role-detail sections load independently. |
| Tailwind | **4.3** | Layout, components, utility classes | CDN-deliverable, no build step. |


### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Microsoft.Extensions.Http` | (in-box with .NET 10 SDK) | `IHttpClientFactory` + typed clients | Always — this is how Studio talks to the Backlot API. Provides pooled handlers and a clean place to attach the auth `DelegatingHandler`. |
| `System.Text.Json` | (in-box) | (De)serialize Backlot API JSON | Always — default serializer. No need for Newtonsoft. The API returns plain JSON (`Body`, `Status`, role objects). |
| ASP.NET Core Session (`AddSession` + `AddDistributedMemoryCache`) | (in-box) | Server-side credential storage | Always — stores the base64 `username:password` server-side so it never reaches the browser (the stated security boundary). |
| ASP.NET Core Cookie Authentication (`AddAuthentication().AddCookie()`) | (in-box) | Auth/redirect gating | Recommended — use a lightweight cookie scheme to drive `[Authorize]` redirects to `/login`, while the *API* credentials live in session. See auth note below. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `dotnet` CLI (.NET 10 SDK) | Build/run/test | `dotnet new razor`, `dotnet run`, add to `Backlot.sln`. |
| LibMan (optional) | Pin CDN assets locally | Only if you want offline/self-hosted Turbo/Bootstrap instead of CDN. Keeps "no npm build" promise. Skip for v1 (CDN is simpler). |
| `dotnet user-secrets` | Local config (API base URL) | Store `BacklotApi:BaseUrl` outside source. Defaults to `https://localhost:7221`. |


## Architecture-Critical Patterns

### 1. API proxying via typed `HttpClient` + auth `DelegatingHandler`

### 2. Turbo + Razor Pages handler convention

- A Razor Page handler can return only the frame's HTML (a partial) for frame requests, detected via the `Turbo-Frame` request header.
- Use `data-turbo-frame` / `data-turbo="false"` attributes to opt specific links in/out of frame targeting.

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
| Bootstrap | No built-in color-mode (dark/light); missing utilities | Tailwind `4.3` |
| `new HttpClient()` directly | Socket exhaustion, DNS-staleness, no auth handler pipeline | `AddHttpClient<T>()` typed client |
| Bare base64 "Basic" with no transport security | Credentials are reversible | Always HTTPS to the API; HttpOnly session cookie |

## Stack Patterns by Variant

- Use LibMan to vendor Tailwind, Turbo, Simulus into `wwwroot/lib`.
- Replace `AddDistributedMemoryCache()` with a distributed cache (Redis via `AddStackExchangeRedisCache`) so session/auth survives across instances.
- Add `Microsoft.Extensions.Http.Resilience` and `.AddStandardResilienceHandler()` to the typed client. Defer for v1.


## Sources

- `https://registry.npmjs.org/@hotwired/turbo/latest` — Turbo 8.0.23 (npm registry, authoritative) — HIGH
- `https://registry.npmjs.org/bootstrap-icons/latest` — Bootstrap Icons 1.13.1 (npm registry, authoritative) — HIGH
- Microsoft .NET blog / Microsoft Learn — .NET 10 GA Nov 2025, LTS to Nov 2028; ASP.NET Core 10 — HIGH
- Microsoft Learn (IHttpClientFactory, DelegatingHandlers) — typed client + auth handler pattern — HIGH
- turbo.hotwired.dev handbook — Turbo Drive/Frames CDN install — MEDIUM
- Backlot `AGENTS.md` — constraints, auth model, API endpoints — HIGH