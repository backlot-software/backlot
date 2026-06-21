<!-- GSD:project-start source:PROJECT.md -->

## Project

**Backlot.Studio**

Backlot.Studio is a standalone .NET Razor Pages web application that serves as the management frontend for the Backlot API. It gives developers and operators a visual interface to browse all registered scenarios, manage persisted roles (search, view details, edit), and inspect role relations — all backed by the Backlot API running alongside it.

**Core Value:** A developer or operator can find any role in the system, inspect its state and relations, and edit it — without writing a single API call by hand.

### Constraints

- **Tech Stack**: .NET Razor Pages + TurboJS + Bootstrap — no React/Vue/SPA framework
- **Project**: Standalone `.csproj` inside `Backlot.Studio/`, added to `Backlot.sln`
- **API**: All data comes from the Backlot API; Studio has no database of its own
- **Auth**: Basic Auth only — username/password encoded as base64, sent on every request

<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->

## Technology Stack

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

# Scaffold the project and add to the solution

# No extra NuGet packages strictly required for v1:

#   IHttpClientFactory, Session, Cookie auth, System.Text.Json are all in the

#   Microsoft.AspNetCore.App / Microsoft.NETCore.App shared frameworks.

# Add ONLY if you opt into resilience/polly later:

# dotnet add package Microsoft.Extensions.Http.Resilience

## Architecture-Critical Patterns

### 1. API proxying via typed `HttpClient` + auth `DelegatingHandler`

### 2. Turbo + Razor Pages handler convention

- Wrap the Scalar side panel and role-detail sections in `<turbo-frame id="...">`.
- A Razor Page handler can return only the frame's HTML (a partial) for frame requests, detected via the `Turbo-Frame` request header.
- Use `data-turbo-frame` / `data-turbo="false"` attributes to opt specific links in/out of frame targeting.

### 3. Scalar side panel init

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

- Use LibMan to vendor Bootstrap, Turbo, and Scalar into `wwwroot/lib`.
- Add a per-request `nonce` to the Scalar inline init script (Scalar supports nonce).
- Replace `AddDistributedMemoryCache()` with a distributed cache (Redis via `AddStackExchangeRedisCache`) so session/auth survives across instances.
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

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:

- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
