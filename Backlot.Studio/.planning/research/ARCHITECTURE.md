# Architecture Research

**Domain:** .NET Razor Pages frontend acting as a session-auth BFF/proxy to a separate Backlot REST API (server-rendered, Turbo-enhanced, no SPA framework)
**Researched:** 2026-06-21
**Confidence:** MEDIUM-HIGH (HttpClient/DelegatingHandler patterns HIGH from MS docs; Turbo+Razor integration MEDIUM — corroborated across Turbo handbook + multiple .NET community packages)

## Standard Architecture

Backlot.Studio is a **thin presentation layer**. It owns no data. Every page is rendered server-side from data fetched from the Backlot API at request time, with the user's Basic Auth credentials held in server-side session and attached to each outbound API call. Turbo upgrades navigation to feel SPA-like without a JS build pipeline.

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                          BROWSER                              │
│   Turbo Drive (intercepts links/forms) + Turbo Frames        │
│   Bootstrap CSS · Scalar side panel (CDN, data-turbo-permanent)│
└───────────────┬──────────────────────────────▲──────────────┘
                │ GET/POST (cookie: .Studio.Session)│ HTML fragments
┌───────────────▼──────────────────────────────┴──────────────┐
│                  BACKLOT.STUDIO (Razor Pages)                 │
├──────────────────────────────────────────────────────────────┤
│  Middleware:  Session → AuthGate (redirect to /login)        │
├──────────────────────────────────────────────────────────────┤
│  Pages (PageModel)   │  Partials / Turbo Frame views          │
│  /login /scenarios   │  _RoleTable _RoleDetail _Relations     │
│  /roles /roles/{uid} │  _ScalarPanel                          │
│  /roles/{uid}/edit   │                                        │
├──────────────────────────────────────────────────────────────┤
│  API Service layer (typed HttpClient wrappers)               │
│  IScenarioApi · IRoleApi · IAuthApi                          │
├──────────────────────────────────────────────────────────────┤
│  BasicAuthHandler (DelegatingHandler) reads creds from session│
│  + IHttpClientFactory typed client (BaseAddress = API url)   │
├──────────────────────────────────────────────────────────────┤
│  Models / DTOs  (envelope { Body, Status, TimeInMs } unwrap) │
└───────────────┬──────────────────────────────────────────────┘
                │ HTTPS + Authorization: Basic base64(user:pass)
┌───────────────▼──────────────────────────────────────────────┐
│                BACKLOT API  (separate process)               │
│   /api/role/director/* · /simplequery/find · /seekbase/detail │
│   /persist/persist · /persist/relations                       │
└───────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| Pages (PageModel) | One per route; orchestrate fetch → bind → render. No HTTP/auth logic inline. | Razor Pages `.cshtml` + `.cshtml.cs`, async `OnGetAsync`/`OnPostAsync` |
| Partial / Frame views | Reusable fragments returned standalone for Turbo Frames or full-page composition | `_Partial.cshtml`, rendered via `<partial>` or `Partial()` result |
| API Service layer | Typed methods (`FindRolesAsync`, `GetDetailAsync`, `PersistAsync`) that call the API and unwrap the `{ Body, Status }` envelope | Interface + class registered as typed `HttpClient` |
| BasicAuthHandler | Attach `Authorization: Basic …` to every outbound request from session creds | `DelegatingHandler` (Transient) reading `IHttpContextAccessor` per call |
| Auth/session | Hold base64 credentials server-side; gate unauthenticated requests | ASP.NET Core Session (Data Protection-backed cookie) + auth middleware |
| Models/DTOs | Map API JSON (envelope + dynamic role shape) to view-friendly types | POCOs; role detail is dynamic → `Dictionary<string,object>`/`JsonElement` |
| Scalar panel | Embedded OpenAPI reference, slide-in overlay | `@scalar/api-reference` CDN `<script>`, persisted across Turbo nav |

## Recommended Project Structure

```
Backlot.Studio/
├── Pages/
│   ├── Shared/
│   │   ├── _Layout.cshtml          # Bootstrap shell, Turbo + Scalar script tags
│   │   ├── _RoleTable.cshtml       # search results table (Turbo Frame target)
│   │   ├── _RoleDetail.cshtml      # fields/permissions/skills fragment
│   │   ├── _Relations.cshtml       # related roles list (lazy Turbo Frame)
│   │   └── _ScalarPanel.cshtml     # side-panel markup (data-turbo-permanent)
│   ├── Login.cshtml(.cs)           # POST creds → validate via isauthenticated → store in session
│   ├── Scenarios.cshtml(.cs)       # director/scenarios overview + Scalar trigger
│   ├── Roles/
│   │   ├── Index.cshtml(.cs)       # simplequery/find search + pagination
│   │   ├── Detail.cshtml(.cs)      # seekbase/detail + persist/relations  (/roles/{uid})
│   │   └── Edit.cshtml(.cs)        # GET detail, POST persist/persist     (/roles/{uid}/edit)
│   └── _ViewImports / _ViewStart
├── Services/
│   ├── IBacklotApiClient.cs        # typed-client interface(s)
│   ├── ScenarioApi.cs / RoleApi.cs / AuthApi.cs
│   └── Envelope<T>.cs              # { Body, Status, TimeInMs, ExecutionTime }
├── Infrastructure/
│   ├── BasicAuthHandler.cs         # DelegatingHandler
│   ├── SessionKeys.cs              # constant keys (Credentials, Username)
│   └── AuthGuardMiddleware.cs      # redirect to /login when no creds
├── Models/                         # request/response DTOs (SimpleQuery, SeekBase, …)
├── wwwroot/
│   ├── js/lib/turbo.min.js         # self-hosted (or via mvdmio.Hotwire.NET)
│   ├── js/site.js                  # Stimulus-lite controllers, Scalar init on turbo:load
│   └── css/                        # Bootstrap + overrides
├── appsettings.json                # Backlot:BaseUrl, Session timeout
└── Program.cs                      # DI: session, typed client, handler, auth middleware
```

### Structure Rationale

- **Pages/ mirrors routes 1:1** — Razor Pages convention; `/roles/{uid}/edit` maps to `Pages/Roles/Edit.cshtml`.
- **Services/ isolates all HTTP** — Pages never `new HttpClient()` or touch headers. Swappable/mockable for tests.
- **Infrastructure/ holds cross-cutting concerns** — the auth handler and session gate are not page-specific.
- **Shared partials double as Turbo Frame fragments** — the same `_RoleTable` renders inside the page and answers a frame request, avoiding duplicate markup.

## Architectural Patterns

### Pattern 1: Typed HttpClient + per-request Basic Auth DelegatingHandler

**What:** Register one (or a few) typed clients with `BaseAddress` = API URL; chain a `BasicAuthHandler` that reads the base64 credentials from session on **every** `SendAsync`.
**When to use:** Always, for this project — it is the canonical IHttpClientFactory pattern.
**Trade-offs:** IHttpClientFactory pools and reuses the underlying handler, so the handler instance is shared. You MUST NOT cache credentials in a handler field — read them fresh per call via `IHttpContextAccessor`, or the wrong user's creds will leak.

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BasicAuthHandler>();
builder.Services.AddHttpClient<IRoleApi, RoleApi>(c =>
        c.BaseAddress = new Uri(builder.Configuration["Backlot:BaseUrl"]!))
    .AddHttpMessageHandler<BasicAuthHandler>();

// BasicAuthHandler.cs
protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) {
    var creds = _ctx.HttpContext?.Session.GetString(SessionKeys.Credentials); // base64 user:pass
    if (creds is not null)
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
    return base.SendAsync(req, ct);
}
```

### Pattern 2: Envelope unwrapping in the service layer

**What:** Every API response is `{ "Body": …, "Status": …, "TimeInMs": … }`. The service layer deserializes to `Envelope<T>` and returns only `Body`, mapping a non-OK `Status` to an exception.
**When to use:** Every call. Keeps Pages free of envelope plumbing.
**Trade-offs:** Role detail `Body` is dynamic (arbitrary fields plus `__Permission`, `__Skills`, `Uid`). Deserialize detail/edit payloads to `Dictionary<string,JsonElement>` rather than fixed POCOs.

### Pattern 3: Turbo Frame fragments for partial updates

**What:** Wrap the results table, detail body, and relations list in `<turbo-frame id="…">`. Search/pagination links and the relations panel target their frame; the PageModel returns just the partial when the `Turbo-Frame` request header is present.
**When to use:** Search/pagination (no full reload), lazy-loaded relations (`<turbo-frame src="/roles/{uid}/relations" loading="lazy">`), and the slide-in detail.
**Trade-offs:** Form POSTs inside frames expect a **303** redirect on success (Turbo follows the redirect into the frame). Returning a 200 with a full page where Turbo expects a frame causes "content missing" errors. Either render the partial directly or `Redirect()` with 303.

```html
<turbo-frame id="role-results">
    <partial name="_RoleTable" model="Model.Page" />
</turbo-frame>
<turbo-frame id="relations" src="/roles/@Model.Uid/relations" loading="lazy">
    Loading relations…
</turbo-frame>
```

## Data Flow

### Request Flow (role search → detail)

```
User submits search  (Turbo intercepts GET, sends Turbo-Frame: role-results)
    ↓
Roles/Index.OnGetAsync(query,page)
    ↓
IRoleApi.FindRolesAsync → typed HttpClient → BasicAuthHandler adds Authorization
    ↓
POST /api/role/simplequery/find  →  Backlot API
    ↓
Envelope<PagedResult> ← unwrap ← JSON response
    ↓
return Partial("_RoleTable")  → Turbo swaps #role-results frame only
```

### Session / Auth Flow

```
Anonymous request
    ↓ AuthGuardMiddleware: no creds in session → 302 /login (skip /login itself)
Login POST (username, password) [antiforgery validated]
    ↓ build base64(user:pass), call GET /director/isauthenticated with it
    ├─ true  → Session.SetString(Credentials, base64); Session.SetString(User, name); redirect /scenarios
    └─ false → re-render /login with error
Subsequent requests → session cookie (Data Protection encrypted) → handler injects creds → API
Logout → Session.Clear() → /login
```

Notes: store **only** the base64 credential blob server-side in session — never in a browser-visible cookie, hidden field, or localStorage. Configure the session cookie `HttpOnly`, `Secure`, `SameSite=Strict`. For a single instance the default in-memory session store is fine; multi-instance needs a distributed cache (Redis/SQL) since creds live server-side.

### Key Data Flows

1. **Authenticated proxy call:** browser cookie → session lookup → handler attaches Basic header → API → envelope unwrap → view. Browser never sees credentials.
2. **Lazy relations:** detail page renders immediately; `<turbo-frame loading="lazy" src=…/relations>` fires a second request that hits `persist/relations`, keeping first paint fast.
3. **Scalar panel:** static CDN script + spec; toggled client-side. Lives outside Turbo's swap region so it survives navigation.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 0-1k users | Single instance, in-memory session, default IHttpClientFactory pooling. No changes needed. |
| 1k-100k users | Move session to distributed cache (Redis) so creds survive across instances; tune `SetHandlerLifetime` and connection pool; enable response compression for HTML fragments. |
| 100k+ users | Studio is just a proxy — the Backlot API is the real bottleneck. Scale Studio horizontally behind a load balancer (sticky sessions optional with shared cache); add a short-TTL cache for `director/scenarios`/`roles` (rarely change). |

### Scaling Priorities

1. **First bottleneck: session affinity.** In-memory session breaks the moment you add a second instance — credentials vanish on the other node. Switch to distributed session before scaling out.
2. **Second bottleneck: API round-trips per page.** Detail page makes detail + relations calls; lazy frames and caching static metadata reduce load.

## Anti-Patterns

### Anti-Pattern 1: Caching credentials/HttpContext in the DelegatingHandler

**What people do:** Store the base64 creds (or `HttpContext`) in a handler field set in the constructor.
**Why it's wrong:** IHttpClientFactory pools and reuses handler instances across requests/users — a cached value leaks one user's credentials to another.
**Do this instead:** Inject `IHttpContextAccessor` and read session inside `SendAsync` on every call.

### Anti-Pattern 2: Returning a full page where Turbo expects a frame / 200 instead of 303

**What people do:** After an edit POST, return the full page with a 200.
**Why it's wrong:** Turbo Frame/Drive form handling expects a **303** redirect on success; a 200 full page into a frame triggers "content missing" or double-rendered chrome.
**Do this instead:** `return RedirectToPage(...)` (303) on success, or render the matching partial directly for the frame.

### Anti-Pattern 3: Loading the Scalar CDN widget inside Turbo's swap region

**What people do:** Put the `@scalar/api-reference` script/element in the page body that Turbo replaces.
**Why it's wrong:** Turbo discards body content on navigation; the widget initializes once and then disappears or fails to re-init, since body scripts re-eval but third-party DOM state is lost.
**Do this instead:** Place the panel element in the layout and mark it `data-turbo-permanent` with a stable `id` (or re-initialize on the `turbo:load` event). Keep it out of frames.

### Anti-Pattern 4: Putting HTTP/auth logic directly in PageModels

**What people do:** `new HttpClient()` and set headers inside `OnGetAsync`.
**Why it's wrong:** Socket exhaustion, no pooling, duplicated auth, untestable pages.
**Do this instead:** All HTTP goes through the typed-client service layer; Pages depend on interfaces.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| Backlot REST API | Typed `HttpClient` + `BasicAuthHandler`, `BaseUrl` from config (`https://localhost:7221`) | All responses are `{ Body, Status, … }` envelopes; role detail body is dynamic. Trust the dev TLS cert in development. |
| Scalar API reference | CDN `<script>` + OpenAPI spec, client-side overlay | Must live outside Turbo swap region (`data-turbo-permanent`). Zero build step. |
| Turbo (Hotwire) | Self-hosted JS in `wwwroot` (or `mvdmio.Hotwire.NET` which copies JS + offers `TurboStream` results) | Avoid a JS bundler; reference the static file. Forms need 303 redirects. |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Page ↔ Service layer | Direct DI call to interface | Pages know nothing about HTTP/envelopes/auth |
| Service ↔ BasicAuthHandler | IHttpClientFactory pipeline | Handler is the only place creds are read |
| Handler ↔ Session | `IHttpContextAccessor` per request | Never cache; read fresh each `SendAsync` |
| Middleware ↔ Pages | Redirect on missing creds | `/login` and static assets must be exempt |

## Suggested Build Order (dependency-driven)

This ordering reflects what each component depends on — it directly informs roadmap phase structure.

1. **Project skeleton + config** — `Backlot.Studio.csproj` in the solution, `Program.cs`, Bootstrap layout, `Backlot:BaseUrl` setting. (No dependencies; everything else needs it.)
2. **API service layer + typed client + envelope** — `IRoleApi`/`IScenarioApi`/`IAuthApi`, `Envelope<T>`. Can be built and unit-tested against the API before any auth UI exists. (Depends on 1.)
3. **Session + BasicAuthHandler + login + auth-guard middleware** — wire creds into the handler; gate routes. (Depends on 2 — login validates via `isauthenticated` through the service layer.)
4. **Scenarios overview page** — first read-only page; proves end-to-end auth + fetch + render. Simple, no Turbo Frames yet. (Depends on 3.)
5. **Roles list (search + pagination)** — introduce Turbo Frames for in-place results. (Depends on 3, 4 for patterns.)
6. **Role detail (fields/permissions/skills + lazy relations frame)** — navigation between related roles. (Depends on 5.)
7. **Role edit** — POST `persist/persist`, antiforgery, 303 redirect on success. (Depends on 6 — reuses detail fetch/bind.)
8. **Scalar side panel** — additive overlay; safest last because of the Turbo-permanent interaction. (Depends on 1; independent of data pages.)

**Critical path:** 1 → 2 → 3 gates everything. Turbo Frame conventions are established in step 5 and reused in 6-7. Scalar (8) can be parallelized.

## Sources

- HTTP requests with IHttpClientFactory — Microsoft Learn (typed clients, DelegatingHandler, handler lifetime/pooling) — HIGH — https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
- Use the IHttpClientFactory — .NET / Microsoft Learn (scope caution, do not cache HttpContext in handlers) — HIGH — https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
- Extending HttpClient With Delegating Handlers — Milan Jovanović — MEDIUM — https://www.milanjovanovic.tech/blog/extending-httpclient-with-delegating-handlers-in-aspnetcore
- Building Your Turbo Application (handbook: script eval, data-turbo-permanent, frames, 303) — Hotwire — MEDIUM — https://turbo.hotwired.dev/handbook/building
- Getting started with Hotwire in ASP.NET — hotwire.io — MEDIUM — https://hotwire.io/frameworks/aspdotnet
- mvdmio.Hotwire.NET (self-hosted Turbo JS, TurboStream results) — NuGet — MEDIUM — https://github.com/mvdmio/Hotwire.NET
- Prevent CSRF/XSRF attacks + Exploring ASP.NET Core cookies (Data Protection, session) — Microsoft Learn / nestenius.se — MEDIUM — https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery

---
*Architecture research for: Razor Pages session-auth BFF/proxy to a REST API*
*Researched: 2026-06-21*
