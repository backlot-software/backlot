# Phase 02: Scenarios & API Explorer — Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core Razor Pages + Hotwired Turbo 8 + Scalar API Reference 1.60.0
**Confidence:** MEDIUM-HIGH (stack is constrained by CLAUDE.md; Scalar/Turbo interaction patterns verified against official docs and live source)

---

## User Constraints (from CONTEXT.md)

No CONTEXT.md exists for Phase 2 yet. Constraints are inherited from project-level decisions:

### Locked Decisions (from CLAUDE.md + Phase 1 CONTEXT.md)
- **D-01:** Left sidebar layout (two-panel Bootstrap flex) — fixed sidebar + main content area
- **D-02:** Sidebar has placeholder nav items for pages not yet shipped; "Scenarios" activates in Phase 2
- **D-03:** Sidebar collapsible via toggle (`data-turbo-permanent` on `<aside id="sidebar">`)
- **D-04/05:** Session idle timeout 8 hours, sliding expiry
- **D-08:** User identity displayed in sidebar (username + logout link)
- **Stack:** .NET 10 Razor Pages, Hotwired Turbo 8.0.23 (CDN), Bootstrap 5.3.8 (CDN), Bootstrap Icons 1.13.1 (CDN), Scalar API Reference 1.60.0 (CDN) — no npm/webpack/Vite build pipeline
- **API creds:** Basic Auth injected via `DelegatingHandler` reading from `ISession`; credentials never reach the browser
- **Scalar:** `Scalar.createApiReference()` JS API only — not the old `data-url` script attribute embed

### Claude's Discretion
- Exact CSS transition for the Scalar panel slide-in animation
- Whether panel close-on-`turbo:before-visit` is implemented for Drive navigations
- How endpoint correlation works — whether clicking "Open API Docs" scrolls to the scenario's first endpoint in Scalar (via hash) or just opens Scalar at the top

### Deferred Ideas (OUT OF SCOPE for Phase 2)
- Running / executing scenarios from Studio (Scalar handles that)
- Category filter/collapse in v1 (static group headings only)
- Mobile-responsive layout

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SCEN-01 | User can view a list of all registered scenarios from `GET /api/role/director/scenarios`, grouped or tagged by category | API response shape confirmed from source; Tags field drives grouping; C# Razor Page renders grouped list synchronously |
| SCEN-02 | User can open a Scalar API reference side panel (slide-in overlay) keyed to any scenario, showing the endpoint's interactive docs | Scalar 1.60.0 CDN embed with `createApiReference()` + `app.destroy()` confirmed; `data-turbo-permanent` pattern confirmed for single-init; endpoint hash navigation pattern documented |
</phase_requirements>

---

## Summary

Phase 2 adds two deliverables on top of the Phase 1 foundation: a Scenarios overview page at `/scenarios` and a slide-in Scalar API reference side panel. Both are read-only and use no form posts, so the Turbo 303/422 complexity is deferred to Phase 4. The primary technical risk in this phase is the Scalar + Turbo Drive interaction: Scalar initializes a complex Vue-based widget inside a CDN bundle, and Turbo Drive swaps the `<body>` without full page reloads. Getting this right without duplication or blanking is the core engineering challenge.

The scenario data comes from `GET /api/role/director/scenarios`, which returns an array of `ScenarioResultItem` objects inside a `Body` envelope. Each item has `Scenario` (name), `Result` (type name), `Roles[]` (string array of role names), `Tags[]` (string array for grouping — derived from C# namespace when not explicit), `Endpoints[]` (URL strings, primary first), and `Configurations[]`. Tags drive the grouping on the UI; scenarios without tags fall into an "Uncategorized" bucket.

The Scalar panel must be mounted once and persisted across Turbo navigations using `data-turbo-permanent`. The `createApiReference()` call returns an app object with `app.destroy()` and `app.updateConfiguration()` methods. The correct pattern is: on `turbo:load`, check if the mount element already has a sentinel flag (`__scalar_initialized`); if not, call `createApiReference()` and set the flag. The `openapidoc.json` file that already exists in the Studio project root must be moved to `wwwroot/openapidoc.json` so it is served as a static asset at `/openapidoc.json`.

**Primary recommendation:** Mount the Scalar panel inside a `<div id="scalar-panel" data-turbo-permanent>` in `_Layout.cshtml`. Initialize once via `turbo:load` with a sentinel guard. Serve `openapidoc.json` from `wwwroot/`. Open the panel via class toggle (`is-open`) in vanilla JS. Deep-link to a specific scenario endpoint via URL hash (`window.location.hash = '#tag/Scenarios/post-api-role-...'`) after the panel opens.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Fetch scenario list | Server (Razor Page) | — | `BacklotApiClient` fetches on `OnGet()`, passes model to view; no client-side fetch needed |
| Render scenario cards with grouping | Server (Razor Pages / cshtml) | — | Server-renders complete HTML; no JS templating |
| Loading/empty/error states | Server (Razor Page) | Client (Turbo progress bar) | Server decides state at render time; Turbo's built-in progress bar handles in-transit indication |
| Scalar panel mount and init | Client (JS, `turbo:load`) | — | Scalar is a client-side Vue widget; must init in browser after DOM is ready |
| Panel open/close toggle | Client (vanilla JS) | — | CSS class toggle (`is-open`), focus trap, Escape key — no server involvement |
| Serve openapidoc.json | Server (StaticFiles middleware) | — | `app.UseStaticFiles()` serves `wwwroot/openapidoc.json` at `/openapidoc.json` |
| Auth guard | Server (middleware / cookie auth) | — | `[Authorize]` on PageModel; session-gated; same as Phase 1 |
| 401 → login redirect | Server (middleware) | Client (Turbo top-level) | Centralized in service layer from Phase 1; force `_top` target to avoid frame conflict |

---

## Standard Stack

### Core (all constrained by CLAUDE.md — no alternatives)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Razor Pages | .NET 10 in-box | Scenario list page (`/scenarios`), `OnGet()` fetches, model binding | Page-centric model maps 1:1 to admin pages; in Phase 1 foundation |
| Hotwired Turbo | 8.0.23 (CDN) | Drive: intercepts link clicks for SPA navigation; `data-turbo-permanent` for Scalar panel persistence | Constraint-mandated; already loaded in `_Layout.cshtml` from Phase 1 |
| Bootstrap | 5.3.8 (CDN) | Scenario card layout, panel overlay, category section headings | Constraint-mandated; already in layout |
| Bootstrap Icons | 1.13.1 (CDN) | Icons for "Open API Docs" button, close button | Constraint-mandated |
| Scalar API Reference | 1.60.0 (CDN) | Interactive OpenAPI docs in slide-in panel | Constraint-mandated |
| `BacklotApiClient` (typed `HttpClient`) | Phase 1 artifact | Fetches from `/api/role/director/scenarios` | Established in Phase 1; `BasicAuthHandler` injects creds |
| `System.Text.Json` | .NET 10 in-box | Deserializes `JsonResponse<IEnumerable<ScenarioResultItem>>` | In-box; no Newtonsoft needed for this simple envelope |

### CDN Script Tags (add to `_Layout.cshtml` in Phase 2)

```html
<!-- Scalar API Reference 1.60.0 — load in <head> so Scalar global is available before turbo:load fires -->
<script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.60.0"></script>
```

The Turbo and Bootstrap CDN tags are already in `_Layout.cshtml` from Phase 1.

**No new NuGet packages required.** All runtime needs are in-box with .NET 10.

---

## Package Legitimacy Audit

No new npm or NuGet packages are installed in Phase 2. All assets come via pinned CDN script/link tags already in the project stack.

| Asset | Channel | Pin | Disposition |
|-------|---------|-----|-------------|
| `@scalar/api-reference@1.60.0` | CDN (jsdelivr) | Exact version | Approved — no install, CDN tag only |
| `@hotwired/turbo@8.0.23` | CDN (jsdelivr) | Exact version | Approved — Phase 1 artifact |
| `bootstrap@5.3.8` | CDN (jsdelivr) | Exact version | Approved — Phase 1 artifact |
| `bootstrap-icons@1.13.1` | CDN (jsdelivr) | Exact version | Approved — Phase 1 artifact |

**Packages removed due to SLOP verdict:** none
**Packages flagged as suspicious SUS:** none

---

## Architecture Patterns

### System Architecture Diagram

```
Browser (Turbo Drive)
  │
  │  GET /scenarios
  ▼
Razor Page: Scenarios/Index.cshtml.cs (OnGet)
  │
  │  GET /api/role/director/scenarios
  ▼
BacklotApiClient (typed HttpClient + BasicAuthHandler)
  │
  ▼
Backlot API → returns JsonResponse<IEnumerable<ScenarioResultItem>>
  │         { Body: [...], Status: "...", TimeInMs: ..., ExecutionTime: ... }
  │
  ▼
PageModel: groups Body by Tags[0] (first tag; "Uncategorized" if empty)
  │         passes ScenarioGroups to Razor view
  │
  ▼
Razor View: renders category headings + scenario cards (full-page HTML)
  │
  ▼
Browser receives full HTML page
  │
  ├── Turbo Drive replaces <body>, preserves data-turbo-permanent elements
  │     - <aside id="sidebar"> (Phase 1)
  │     - <div id="scalar-panel"> (Phase 2)
  │
  └── turbo:load fires
        └── JS checks #scalar-panel.__scalar_initialized
              ├── false → Scalar.createApiReference('#scalar-mount', { url: '/openapidoc.json' })
              │           sets #scalar-panel.__scalar_initialized = true
              └── true  → no-op (panel persisted via data-turbo-permanent)

User clicks "Open API Docs" on scenario card
  │
  └── vanilla JS:
        1. document.getElementById('scalar-panel').classList.add('is-open')
        2. show backdrop
        3. optionally: window.location.hash = '#tag/Scenarios/post-api-role-rolename-scenarioname'
        4. trap focus inside panel
```

### Recommended Project Structure (Phase 2 additions)

```
Backlot.Studio/
├── wwwroot/
│   ├── openapidoc.json          ← move from project root (or copy via build)
│   ├── css/
│   │   └── studio.css           ← Phase 1 custom CSS; add .scalar-panel, .is-open, backdrop rules
│   └── js/
│       └── studio.js            ← Phase 1 JS; add Scalar init guard + panel open/close logic
├── Pages/
│   ├── Shared/
│   │   ├── _Layout.cshtml       ← Phase 1; ADD: Scalar CDN script, #scalar-panel div
│   │   ├── _Sidebar.cshtml      ← Phase 1; EXTEND: mark Scenarios nav item as active
│   │   └── _ViewImports.cshtml  ← Phase 1
│   └── Scenarios/
│       ├── Index.cshtml         ← NEW: scenario list page
│       └── Index.cshtml.cs      ← NEW: PageModel with OnGet()
├── Models/
│   └── Api/
│       ├── ScenarioItem.cs      ← NEW: C# DTO matching ScenarioResultItem JSON
│       └── ApiEnvelope.cs       ← Phase 1 artifact (Envelope<T> with Body, Status, TimeInMs)
└── Services/
    └── BacklotApiClient.cs      ← Phase 1; ADD: GetScenariosAsync() method
```

### Pattern 1: API Envelope Deserialization

The Backlot API wraps every response in `JsonResponse<T>`:
```json
{ "Body": [...], "Status": "OK", "TimeInMs": 42, "ExecutionTime": "..." }
```

The Phase 1 `ApiEnvelope<T>` model handles this. For scenarios:

```csharp
// Source: Backlot.Http/Media/Formatters/JsonResponse.cs (internal shape)
public class ApiEnvelope<T>
{
    public T? Body { get; set; }
    public string? Status { get; set; }
    public long TimeInMs { get; set; }
}

public class ScenarioItem
{
    public string Scenario { get; set; } = null!;   // scenario name
    public string Result { get; set; } = null!;     // result type friendly name
    public string[] Roles { get; set; } = [];       // role names (strings)
    public string[] Tags { get; set; } = [];        // grouping tags (from namespace if not explicit)
    public string[] Endpoints { get; set; } = [];   // URL paths; primary/most-important first
    public string[] Configurations { get; set; } = []; // named config variants
}
```

### Pattern 2: Grouping Scenarios by Tag in PageModel

```csharp
// Source: ScenarioAttribute.cs — Tags defaults to [last segment of namespace]
// when Tags is empty, use "Uncategorized" bucket

public class IndexModel : PageModel
{
    private readonly BacklotApiClient _api;
    public List<(string Category, IEnumerable<ScenarioItem> Scenarios)> Groups { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public IndexModel(BacklotApiClient api) => _api = api;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var result = await _api.GetScenariosAsync();
            Groups = (result ?? [])
                .GroupBy(s => s.Tags.Length > 0 ? s.Tags[0] : "Uncategorized")
                .Select(g => (g.Key, g.AsEnumerable()))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable.";
        }
        return Page();
    }
}
```

**Why first tag only:** `ScenarioAttribute` sets `Tags = [namespace.Split(".").Last()]` when not explicit — a single-element array. Multi-tag scenarios are rare; using `Tags[0]` with "Uncategorized" fallback is correct for v1.

### Pattern 3: Scalar Init/Teardown Under Turbo (CRITICAL)

This is the highest-risk pattern in the phase. [CITED: turbo.hotwired.dev/handbook/building, scalar.com/products/api-references/integrations/html-js]

```html
<!-- In _Layout.cshtml — the permanent panel container -->
<div id="scalar-panel" data-turbo-permanent
     style="position:fixed; top:0; right:0; width:min(480px,100vw); height:100vh; z-index:1055;
            transform:translateX(100%); transition:transform .25s ease;">
  <button class="btn-close position-absolute top-0 end-0 m-2"
          aria-label="Close API Reference panel"
          onclick="document.getElementById('scalar-panel').classList.remove('is-open')">
  </button>
  <div id="scalar-mount"></div>
</div>
<div id="scalar-backdrop" data-turbo-permanent
     style="display:none; position:fixed; inset:0; background:rgba(0,0,0,.4); z-index:1054;"
     onclick="document.getElementById('scalar-panel').classList.remove('is-open'); this.style.display='none'">
</div>
```

```javascript
// In studio.js or inline <script> at bottom of _Layout.cshtml
// Source: scalar.com/products/api-references/integrations/html-js + turbo.hotwired.dev/handbook/building
let scalarApp = null;

document.addEventListener('turbo:load', function () {
  const panel = document.getElementById('scalar-panel');
  if (!panel) return;

  // Guard: only initialize once — data-turbo-permanent means the node persists,
  // but turbo:load fires on every navigation.
  if (panel.dataset.scalarInitialized) return;

  scalarApp = Scalar.createApiReference('#scalar-mount', {
    url: '/openapidoc.json',
    darkMode: false,
    defaultOpenAllTags: false,
  });
  panel.dataset.scalarInitialized = 'true';
});

// Close panel before Turbo Drive navigates away (Drive replaces body; panel stays via permanent,
// but the open state should reset if the user navigates to a non-scenarios page)
document.addEventListener('turbo:before-visit', function () {
  const panel = document.getElementById('scalar-panel');
  if (panel) panel.classList.remove('is-open');
  const backdrop = document.getElementById('scalar-backdrop');
  if (backdrop) backdrop.style.display = 'none';
});

function openScalarPanel(endpointPath) {
  const panel = document.getElementById('scalar-panel');
  const backdrop = document.getElementById('scalar-backdrop');
  panel.classList.add('is-open');
  backdrop.style.display = 'block';

  // Optional: scroll Scalar to the specific endpoint via URL hash
  // Hash format: #tag/TagName/method-api-role-rolename-scenarioname
  if (endpointPath) {
    // Build the hash from the endpoint path string
    // e.g. "/api/role/director/dummy" → "#tag/Scenarios/post-api-role-director-dummy"
    // NOTE: This is best-effort; Scalar's hash format is internal. Simplest: just open at top.
  }
}
```

```css
/* In studio.css */
#scalar-panel.is-open {
  transform: translateX(0);
}
```

### Pattern 4: Scenario Card Razor Markup

```html
@* Scenarios/Index.cshtml *@
@if (Model.ErrorMessage != null)
{
  <div class="alert alert-danger">
    @Model.ErrorMessage <a href="/scenarios">Retry</a>
  </div>
}
else if (!Model.Groups.Any())
{
  <div class="text-center mt-5">
    <h5>No scenarios registered</h5>
    <p class="text-muted">The Backlot API returned an empty scenario list.
       Check that your API is running and has registered scenarios.</p>
  </div>
}
else
{
  @foreach (var (category, scenarios) in Model.Groups)
  {
    <h6 class="text-muted text-uppercase border-bottom pb-1 mt-4">@category</h6>
    <ul class="list-unstyled">
      @foreach (var s in scenarios)
      {
        <li class="card mb-2 p-3">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <h5 class="mb-1">@s.Scenario</h5>
              <small class="text-muted">Returns: @s.Result</small>
              @foreach (var role in s.Roles)
              {
                <span class="badge bg-secondary ms-1">@role</span>
              }
              @foreach (var tag in s.Tags)
              {
                <span class="badge bg-light text-dark ms-1">@tag</span>
              }
            </div>
            <button class="btn btn-primary btn-sm"
                    style="min-height:44px"
                    onclick="openScalarPanel('@s.Endpoints.FirstOrDefault()')">
              Open API Docs
            </button>
          </div>
        </li>
      }
    </ul>
  }
}
```

### Pattern 5: Serving openapidoc.json as a Static File

```csharp
// Source: learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files
// Program.cs — already present from Phase 1; no change needed if file is in wwwroot/
app.UseStaticFiles(); // serves wwwroot/openapidoc.json as /openapidoc.json
```

The `openapidoc.json` file currently lives at the Studio project root, not in `wwwroot/`. Two options:

**Option A (recommended):** Copy `openapidoc.json` into `wwwroot/openapidoc.json` at the start of Phase 2. Simple, no config changes.

**Option B (if the file must stay at project root):** Add a `PhysicalFileProvider` to serve the root directory:
```csharp
// Program.cs — only if Option A is not taken
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = ""
});
```
Option B risks accidentally exposing `appsettings.json` and other root files. Use Option A.

### Anti-Patterns to Avoid

- **Double-initialization of Scalar:** Calling `createApiReference()` on every `turbo:load` without the sentinel flag causes the Scalar Vue app to mount multiple times into `#scalar-mount`, producing duplicated or blank UI. [CITED: turbo.hotwired.dev/handbook/building]
- **Using `DOMContentLoaded` for Scalar init:** Fires only on first hard load, not on Turbo navigations. Always use `turbo:load`. [CITED: turbo.hotwired.dev/handbook/building]
- **Not setting `data-turbo-permanent` on the panel:** Without it, Turbo replaces the panel `<div>` on every navigation and the Scalar instance is torn down and lost.
- **Omitting `id` from the permanent element:** `data-turbo-permanent` requires a stable `id` attribute; Turbo matches elements by ID. [CITED: turbo.hotwired.dev/handbook/building]
- **Serving `openapidoc.json` from the project root without `UseStaticFiles` configuration:** The default middleware only serves `wwwroot/`. The file will 404 until moved.
- **Using `@latest` CDN URLs:** A Scalar release can break the panel with zero local changes. Pin to `@1.60.0`. [CITED: CLAUDE.md]
- **Storing panel open/close state in a Turbo-swapped element:** `is-open` class on a non-permanent element resets on every navigation. The panel `<div>` must be permanent.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| OpenAPI interactive docs | Custom swagger/OpenAPI viewer | Scalar 1.60.0 CDN | Scalar handles spec parsing, auth testing, code samples, dark mode — weeks of work |
| SPA navigation | Manual fetch + DOM swap | Turbo Drive (already loaded) | Turbo handles history, progress bar, scroll restoration |
| Focus trap for panel | Custom focus-trap logic | CSS `inert` attribute on background content | `inert` is supported in all modern browsers; prevents focus leaving the panel without JS libraries |
| HTTP client with retry | Custom retry/backoff code | `Microsoft.Extensions.Http.Resilience` (defer to v2) | `AddStandardResilienceHandler()` handles exponential backoff; not needed for v1 read-only page |

**Key insight:** The Scalar panel delivers 90% of SCEN-02 for free — don't fight its defaults or try to replicate its functionality.

---

## Common Pitfalls

### Pitfall 1: Scalar Panel Blank or Duplicated After Turbo Navigation

**What goes wrong:** User opens `/scenarios`, panel works. User navigates to another page and back. Panel is blank or shows two stacked Scalar UIs.

**Why it happens:** `createApiReference()` mounts a Vue app into `#scalar-mount`. If called twice (no sentinel), two Vue apps fight over the same DOM node. If the panel isn't `data-turbo-permanent`, Turbo replaces it on every navigation and the JS reference to `scalarApp` is stale.

**How to avoid:** `data-turbo-permanent` on `#scalar-panel` + sentinel flag (`panel.dataset.scalarInitialized`) checked in `turbo:load`. [CITED: turbo.hotwired.dev/handbook/building, scalar.com/products/api-references/integrations/html-js]

**Warning signs:** Panel works on first load only; console shows Vue mounting errors; two sets of Scalar sidebar items visible.

### Pitfall 2: openapidoc.json Returns 404

**What goes wrong:** Scalar loads but shows "Failed to load spec" or a network error. The panel is blank.

**Why it happens:** `openapidoc.json` is at the project root, not in `wwwroot/`. `app.UseStaticFiles()` only serves `wwwroot/` by default.

**How to avoid:** Copy/move `openapidoc.json` into `wwwroot/`. Verify `GET /openapidoc.json` returns 200 with `Content-Type: application/json` before wiring Scalar. [CITED: learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files]

**Warning signs:** Scalar shows spinner that never resolves; browser dev tools show 404 on `/openapidoc.json`.

### Pitfall 3: Scenario Grouping Shows All Scenarios in "Uncategorized"

**What goes wrong:** All scenarios appear under a single "Uncategorized" group.

**Why it happens:** `ScenarioAttribute` derives `Tags` from the last segment of the class namespace when `tags` param is not specified. In the Demo.Web scenario `Scenarios.cs` namespace `Backlot.Demo.Web.Scenarios`, the tag would be `Scenarios`. If the API is a minimal deployment where all scenarios are in a flat namespace, Tags may genuinely be empty.

**How to avoid:** Accept whatever Tags the API returns. Always use `Tags.Length > 0 ? Tags[0] : "Uncategorized"` as the group key. If everything falls into one group, that's valid API behavior — don't special-case it. [VERIFIED: Backlot.Core/Abstraction/Scenarios/ScenarioAttribute.cs codebase read]

**Warning signs:** All cards appear under a single heading.

### Pitfall 4: Scalar Hash Navigation Doesn't Scroll to the Right Endpoint

**What goes wrong:** After clicking "Open API Docs", the panel opens at the top of the Scalar reference, not at the specific endpoint for the clicked scenario.

**Why it happens:** Scalar's URL hash format for operations is internal and version-dependent. The format is roughly `#tag/TagName/method-path` but the exact slug generation (spaces → hyphens, role name casing) is controlled by `generateOperationSlug` which defaults to Scalar's internal convention. Setting `window.location.hash` may race against Scalar's own routing.

**How to avoid:** For v1, open the panel at the top level — don't attempt to deep-link. If deep linking is desired in future, use Scalar's `onSidebarClick` callback to discover the actual hash format at runtime and use `updateConfiguration` or `pathRouting`. [CITED: scalar.com/products/api-references/configuration]

**Warning signs:** Hash in URL doesn't match Scalar's sidebar item; Scalar jumps to wrong operation; page URL changes unexpectedly.

### Pitfall 5: `turbo:before-visit` Fires for Frame Navigations

**What goes wrong:** Closing the panel on `turbo:before-visit` closes it even during Turbo Frame navigations within the scenarios page, causing jarring UI.

**Why it happens:** `turbo:before-visit` fires for all Drive navigations (full page visits), not frame navigations. In Phase 2 there are no Turbo Frames on the scenarios page, so this is a non-issue in Phase 2 — but worth noting for Phase 3.

**How to avoid:** In Phase 2, close panel on `turbo:before-visit`. In Phase 3, check if the navigation target is within the current page before closing. [CITED: turbo.hotwired.dev/handbook/building]

---

## API Response: Scenarios Endpoint

Confirmed from reading `Backlot.Defaults/Scenarios/Configuration/Scenarios.cs` and `Models/ScenarioResultItem.cs`: [VERIFIED: codebase]

```
GET /api/role/director/scenarios

Response envelope:
{
  "Body": [
    {
      "Scenario": "Dummy",              // scenario class name
      "Result": "string",              // TResult friendly name
      "Roles": ["Persist", "Formula"], // role names (strings), from constructor params
      "Tags": ["Scenarios"],           // defaults to [last namespace segment]
      "Endpoints": [
        "/api/role/director/dummy",    // director endpoint (when multiple roles)
        "/api/role/persist/dummy"      // primary role endpoint
      ],
      "Configurations": []             // named config variants (empty when none)
    }
  ],
  "TimeInMs": 12,
  "ExecutionTime": "2026-06-21T...",
  "Status": "OK"
}
```

**Key facts confirmed from source:**
- `Roles` is `string[]` (role names, not objects) [VERIFIED: ScenarioResultItem.cs]
- `Tags` defaults to `[namespace.Split(".").Last()]` when `tags` param is null [VERIFIED: ScenarioAttribute.cs]
- `Endpoints` is ordered: director endpoint first (when multi-role), then primary role endpoint [VERIFIED: Scenarios.cs]
- The `openapidoc.json` OpenAPI spec shows paths tagged with `"Scenarios"`, `"Configuration"`, `"Query"`, `"Persistance"`, `"Authentication"` — these are the OpenAPI tags, not the ScenarioResultItem Tags [VERIFIED: openapidoc.json]

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Scalar `data-url` script attribute embed | `Scalar.createApiReference()` JS API | Scalar 1.x | Old attribute ignored in 1.x; must use JS API |
| `DOMContentLoaded` for Turbo-safe JS init | `turbo:load` event | Turbo 7+ | `DOMContentLoaded` fires once; `turbo:load` fires per navigation |
| Turbolinks `data-turbolinks-permanent` | Turbo `data-turbo-permanent` | Turbo 7 (Turbolinks → Turbo rename) | Attribute renamed; behavior identical |
| Swagger UI CDN embed | Scalar CDN embed | Project constraint | Scalar has better UX, dark mode, no jQuery dependency |

**Deprecated/outdated:**
- `data-url` attribute on Scalar's `<script>` tag: ignored in Scalar 1.x; produces no output silently
- `DOMContentLoaded` for any initialization that needs to survive Turbo navigation: replaced by `turbo:load`

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | Build/run Studio | ✓ | 10.0.109 | — |
| `dotnet` CLI | Build commands | ✓ | 10.0.109 | — |
| Backlot API (for testing) | Runtime scenario data | [assumed running on dev] | varies | Use mock data / static JSON for UI development |
| Internet/CDN access | Bootstrap, Turbo, Scalar CDN | [assumed, dev machine] | — | Use LibMan for self-hosting if offline |

**Missing dependencies with no fallback:** None — the project has .NET 10 and the required in-box SDK.

**Note:** Phase 2 depends on Phase 1 (project scaffold, typed API client, `_Layout.cshtml` shell). If Phase 1 is not yet executed, Plan 02-01 and 02-02 cannot proceed. The Phase 1 plan artifacts exist (CONTEXT.md, DISCUSSION-LOG.md) but no `*.csproj` exists yet, confirming Phase 1 is not executed.

---

## Security Domain

`security_enforcement: true`, `security_asvs_level: 1` per `.planning/config.json`.

### Applicable ASVS Categories (Phase 2 specific)

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | Yes (inherited) | Cookie auth from Phase 1; `[Authorize]` on `IndexModel` |
| V3 Session Management | Yes (inherited) | Phase 1 session config (HttpOnly, Secure, SameSite) |
| V4 Access Control | No | Scenarios page is read-only; no role-gated mutation |
| V5 Input Validation | Minimal | No user input on this page; scenario list is API-driven |
| V6 Cryptography | No | No crypto in Phase 2 |

### Phase 2 Security Considerations

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| `openapidoc.json` served publicly | Information Disclosure | Acceptable — the spec documents public API; no secrets in the spec |
| Scalar CDN script loaded | Supply Chain | Pin to `@1.60.0` + SRI hash on the script tag; do not use `@latest` |
| `onclick="openScalarPanel('@s.Endpoints.FirstOrDefault()')"` — inline string injection | XSS | Razor auto-encodes string interpolation in HTML attributes; `@s.Endpoints[0]` is API-sourced (no user control). Mark as low risk but verify Razor encodes the `onclick` value. |
| Basic Auth header proxied through typed client | Spoofing/Info Disclosure | Phase 1 control (DelegatingHandler reads session, not request); no change in Phase 2 |

**SRI integrity hashes:** The UI-SPEC requires SRI hashes on CDN assets. The executor must fetch the canonical SRI hash for `@scalar/api-reference@1.60.0` from jsdelivr during implementation:
```
https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.60.0/dist/browser/standalone.min.js?sri=1
```

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Phase 1 is fully executed before Phase 2 starts (project scaffold, `_Layout.cshtml`, `BacklotApiClient`, auth pages all exist) | Environment Availability | Phase 2 plans reference Phase 1 artifacts; if Phase 1 isn't done, nothing compiles |
| A2 | The Backlot API returns `Tags` as non-empty for most scenarios (derived from namespace) so grouping is meaningful | API Response section | If all Tags are empty, all scenarios land in "Uncategorized" — UI works but grouping is cosmetic |
| A3 | `openapidoc.json` at Studio project root is the correct and current spec file for the running Backlot API | Architecture Patterns — static file | If the spec is stale or for a different API, Scalar shows incorrect endpoints; not a runtime error, just wrong docs |
| A4 | Scalar 1.60.0's `createApiReference()` global is synchronously available after the CDN `<script>` tag loads | Pattern 3 | If Scalar defers global registration, the `turbo:load` handler runs before `Scalar` is defined; mitigate by checking `typeof Scalar !== 'undefined'` in the guard |
| A5 | `data-turbo-permanent` with a stable `id` correctly preserves the Scalar panel across all Turbo Drive navigations in Turbo 8.0.23 | Architecture Pattern 3 | Known Turbo issue: `data-turbo-permanent` has reported edge cases with Turbo Streams (not used in Phase 2); Drive-only navigation should work |

---

## Open Questions

1. **Does `openapidoc.json` need to be kept in sync with the live API?**
   - What we know: The file exists at the Studio project root and is used by Scalar for display only.
   - What's unclear: Is this file manually updated, or is there a build/CI step that regenerates it from the live API?
   - Recommendation: For Phase 2, copy it to `wwwroot/` and document it as "manually maintained". Automate sync in a future phase if needed.

2. **Should "Open API Docs" scroll Scalar to the specific endpoint for the clicked scenario?**
   - What we know: Scalar uses an internal hash format (`#tag/TagName/method-slug`); the exact slug is version-specific.
   - What's unclear: Whether the UI-SPEC expectation "scroll Scalar to the relevant endpoint" is a hard requirement or a nice-to-have.
   - Recommendation: Implement v1 with panel opening at the top level. Add hash navigation as an enhancement once the hash format is confirmed against the pinned 1.60.0 build.

3. **Is Phase 1 (foundation scaffold) expected to be completed in the same planning session, or is Phase 2 planned independently to be executed after Phase 1?**
   - What we know: STATE.md says "stopped at: Phase 2 UI-SPEC approved" and no csproj exists — Phase 1 has not been executed.
   - What's unclear: Execution order — will the planner produce plans for Phase 2 that depend on Phase 1 being done first?
   - Recommendation: Phase 2 plans should state "depends on Phase 1 execution" as an explicit prerequisite and call out Phase 1 artifacts (`_Layout.cshtml`, `BacklotApiClient`) by name.

---

## Sources

### Primary (HIGH confidence)
- `Backlot.Defaults/Scenarios/Configuration/Scenarios.cs` — exact ScenarioResultItem shape, grouping logic, Tags derivation [VERIFIED: codebase]
- `Backlot.Defaults/Scenarios/Configuration/Models/ScenarioResultItem.cs` — C# DTO shape [VERIFIED: codebase]
- `Backlot.Core/Abstraction/Scenarios/ScenarioAttribute.cs` — Tags defaults to namespace last segment [VERIFIED: codebase]
- `Backlot.Http/Media/Formatters/JsonResponse.cs` — API response envelope shape (Body, Status, TimeInMs) [VERIFIED: codebase]
- `Backlot.Studio/openapidoc.json` — 17 paths confirmed; director/scenarios confirmed GET endpoint [VERIFIED: codebase]
- `.planning/phases/01-foundation-auth/01-CONTEXT.md` — D-01 through D-09 locked decisions [VERIFIED: codebase]
- `.claude/CLAUDE.md` — full stack constraints, CDN versions, auth pattern [VERIFIED: codebase]
- `.planning/phases/02-scenarios-api-explorer/02-UI-SPEC.md` — UI design contract (component inventory, layout, interaction) [VERIFIED: codebase]

### Secondary (MEDIUM confidence)
- [scalar.com — HTML/JS Integration](https://scalar.com/products/api-references/integrations/html-js) — `createApiReference()` signature, `app.destroy()`, CSP nonce pattern [CITED]
- [scalar.com — Configuration](https://scalar.com/products/api-references/configuration) — `onLoaded`, `onSidebarClick`, `defaultOpenFirstTag`, `pathRouting`, `generateOperationSlug` [CITED]
- [turbo.hotwired.dev — Building](https://turbo.hotwired.dev/handbook/building) — `data-turbo-permanent` rules (requires `id`), `turbo:load`, `turbo:before-cache`, idempotent init pattern [CITED]
- [learn.microsoft.com — Static Files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0) — `app.UseStaticFiles()` serves `wwwroot/` by default; `PhysicalFileProvider` for custom roots [CITED]

### Tertiary (LOW confidence)
- WebSearch: Scalar destroy/re-init for SPA integration — confirmed `app.destroy()` exists and fixes listener leaks [ASSUMED for exact v1.60.0 behavior]
- WebSearch: Turbo `data-turbo-permanent` third-party JS patterns — idempotent sentinel flag approach [ASSUMED pattern, consistent with docs]

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — stack is fully locked in CLAUDE.md; no discovery needed
- API response shape: HIGH — read directly from Backlot.Core source (ScenarioResultItem, ScenarioAttribute, JsonResponse)
- Scalar + Turbo integration: MEDIUM — Scalar docs confirmed `createApiReference()`, `app.destroy()`, `data-turbo-permanent` confirmed in Turbo handbook; exact `turbo:load` + sentinel pattern is community-validated but not in Scalar's own Turbo guide
- Hash deep-linking to endpoint: LOW — hash format is internal to Scalar; confirmed `generateOperationSlug` exists but exact 1.60.0 format not verified

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (30 days; Scalar CDN versions stable on pinned URLs)
