# Phase 2: Scenarios & API Explorer — Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 8 new/modified files
**Analogs found:** 0 / 8 (codebase is greenfield — Phase 1 not yet executed, no Razor Pages exist)

## Situation Note

Phase 1 has not been executed. The `Backlot.Studio/` directory contains only `openapidoc.json`. No `.csproj`, no `Program.cs`, no `_Layout.cshtml`, no `BacklotApiClient` exist yet. The Backlot solution contains no Razor Pages anywhere — `Backlot.Demo.Web` is a Backlot API host (Minimal API), not a Razor Pages app.

**Consequence:** All analog excerpts in this document are drawn from:
1. The authoritative patterns in `02-RESEARCH.md` (which itself cites Scalar docs, Turbo handbook, and ASP.NET Core docs — all HIGH-confidence sources)
2. `Backlot.Http/Media/Formatters/JsonResponse.cs` — the real API envelope shape (read from source)
3. `Backlot.WebApp/ApplicationBuilding.cs` — real `Program.cs`-style DI/middleware wiring pattern

The planner must treat all patterns below as "first-instance patterns" — they define the canonical style this project will follow going forward, not copies of existing code.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Pages/Scenarios/Index.cshtml.cs` | page model | request-response | none in codebase | no analog — use RESEARCH.md Pattern 2 |
| `Pages/Scenarios/Index.cshtml` | view/template | request-response | none in codebase | no analog — use RESEARCH.md Pattern 4 |
| `Pages/Shared/_Layout.cshtml` | layout | request-response | none in codebase | no analog — use RESEARCH.md Patterns 3 + CDN section |
| `Pages/Shared/_Sidebar.cshtml` | partial view | request-response | none in codebase | no analog — use Phase 1 CONTEXT.md D-01/D-02/D-08 |
| `Models/Api/ScenarioItem.cs` | model/DTO | transform | `Backlot.Http/Media/Formatters/JsonResponse.cs` | shape-match (envelope source) |
| `Models/Api/ApiEnvelope.cs` | model/DTO | transform | `Backlot.Http/Media/Formatters/JsonResponse.cs` | exact shape source |
| `Services/BacklotApiClient.cs` | service | request-response | `Backlot.WebApp/ApplicationBuilding.cs` | partial (DI wiring reference only) |
| `wwwroot/js/studio.js` | client utility | event-driven | none in codebase | no analog — use RESEARCH.md Pattern 3 |
| `wwwroot/css/studio.css` | stylesheet | — | none in codebase | no analog — use UI-SPEC spacing/color tables |
| `wwwroot/openapidoc.json` | static asset | — | `openapidoc.json` (project root) | move — same file, new location |

---

## Pattern Assignments

### `Models/Api/ApiEnvelope.cs` (DTO, transform)

**Analog source:** `Backlot.Http/Media/Formatters/JsonResponse.cs` (lines 1–17)

The API returns this envelope for every response. Studio must deserialize the `Body` field. The internal class is the ground truth for property names:

```csharp
// Source: Backlot.Http/Media/Formatters/JsonResponse.cs lines 1-17
// Property names are PascalCase — System.Text.Json default matches this.
public DateTimeOffset ExecutionTime => DateTimeOffset.Now;
public long TimeInMs { get; }
public T Body { get; set; }
public string Status { get; set; } = null!;
```

Studio's public DTO version:

```csharp
// Models/Api/ApiEnvelope.cs — copy this shape exactly
namespace Backlot.Studio.Models.Api;

public class ApiEnvelope<T>
{
    public T? Body { get; set; }
    public string? Status { get; set; }
    public long TimeInMs { get; set; }
    public DateTimeOffset ExecutionTime { get; set; }
}
```

---

### `Models/Api/ScenarioItem.cs` (DTO, transform)

**Source:** `02-RESEARCH.md` §Pattern 1 (verified against `Backlot.Defaults/Scenarios/Configuration/Models/ScenarioResultItem.cs`)

```csharp
// Models/Api/ScenarioItem.cs
namespace Backlot.Studio.Models.Api;

public class ScenarioItem
{
    public string Scenario { get; set; } = null!;       // scenario class name
    public string Result { get; set; } = null!;         // TResult friendly name
    public string[] Roles { get; set; } = [];           // role names
    public string[] Tags { get; set; } = [];            // grouping tags (namespace-derived when not explicit)
    public string[] Endpoints { get; set; } = [];       // URL paths; director endpoint first when multi-role
    public string[] Configurations { get; set; } = [];  // named config variants
}
```

**Key fact from source verification:** `Roles` is `string[]` (not objects). `Tags` defaults to `[namespace.Split(".").Last()]` when not explicit. `Endpoints[0]` is the director endpoint when the scenario has multiple roles.

---

### `Services/BacklotApiClient.cs` (service, request-response)

**Analog source:** `Backlot.WebApp/ApplicationBuilding.cs` lines 33–43 (DI wiring pattern — shows `builder.Services.Add*` registration style used in this solution)

**DI registration pattern** (from `ApplicationBuilding.cs` lines 33–43):
```csharp
// The Backlot solution registers services on builder.Services before builder.Build().
// Studio follows the same pattern in Program.cs.
builder.Services.AddHttpClient<BacklotApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BacklotApi:BaseUrl"]
                                 ?? "https://localhost:7221");
})
.AddHttpMessageHandler<BasicAuthHandler>();
```

**`BasicAuthHandler` pattern** (from `02-RESEARCH.md` §Architecture-Critical Patterns):
```csharp
// Services/BasicAuthHandler.cs
public class BasicAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BasicAuthHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var encoded = session?.GetString("BasicAuthCredentials");
        if (!string.IsNullOrEmpty(encoded))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", encoded);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

**`GetScenariosAsync` method pattern** (from `02-RESEARCH.md` §Pattern 2):
```csharp
// Services/BacklotApiClient.cs
public async Task<IEnumerable<ScenarioItem>?> GetScenariosAsync()
{
    var response = await _httpClient.GetAsync("api/role/director/scenarios");
    response.EnsureSuccessStatusCode();
    var envelope = await response.Content
        .ReadFromJsonAsync<ApiEnvelope<IEnumerable<ScenarioItem>>>();
    return envelope?.Body;
}
```

---

### `Pages/Scenarios/Index.cshtml.cs` (page model, request-response)

**Analog:** None in codebase. Pattern from `02-RESEARCH.md` §Pattern 2.

**Imports pattern:**
```csharp
using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
```

**Auth guard pattern** (`[Authorize]` on PageModel — cookie auth from Phase 1 drives redirect to `/Login`):
```csharp
[Authorize]
public class IndexModel : PageModel
```

**Core OnGet pattern:**
```csharp
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
        ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable and that your credentials are valid.";
    }
    return Page();
}
```

**Error handling pattern:** Catch `HttpRequestException | TaskCanceledException` only (API-layer errors). Let other exceptions propagate to the framework error handler. Do NOT swallow `UnauthorizedAccessException` — that must surface as a 401 → redirect.

---

### `Pages/Scenarios/Index.cshtml` (view, request-response)

**Analog:** None in codebase. Pattern from `02-RESEARCH.md` §Pattern 4 and `02-UI-SPEC.md` §Layout Contract.

**Page header:**
```html
@page
@model Backlot.Studio.Pages.Scenarios.IndexModel
@{
    ViewData["Title"] = "Scenarios";
    ViewData["ActiveNav"] = "scenarios";
}

<h4 class="fw-semibold mb-4">Scenarios</h4>
```

**Error state** (Bootstrap `alert-danger`, per UI-SPEC §States):
```html
@if (Model.ErrorMessage != null)
{
    <div class="alert alert-danger" role="alert">
        @Model.ErrorMessage <a href="/scenarios" class="alert-link">Retry</a>
    </div>
}
```

**Empty state** (per UI-SPEC §States — centered, no CTA):
```html
else if (!Model.Groups.Any())
{
    <div class="text-center mt-5">
        <h5 class="fw-semibold">No scenarios registered</h5>
        <p class="text-muted">The Backlot API returned an empty scenario list.
            Check that your API is running and has registered scenarios.</p>
    </div>
}
```

**Category group + scenario card list** (per UI-SPEC §Layout Contract and `02-RESEARCH.md` §Pattern 4):
```html
else
{
    @foreach (var (category, scenarios) in Model.Groups)
    {
        <h6 class="text-muted text-uppercase border-bottom pb-1 mt-4 fw-semibold"
            style="letter-spacing:.05em">@category</h6>
        <ul class="list-unstyled">
            @foreach (var s in scenarios)
            {
                <li class="card mb-2 p-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <h5 class="mb-1 fw-semibold">@s.Scenario</h5>
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

**XSS note:** Razor auto-encodes `@s.Endpoints.FirstOrDefault()` in the `onclick` attribute. The endpoint strings come from the Backlot API (not user input). Risk is low, but encoding is correct.

---

### `Pages/Shared/_Layout.cshtml` (layout, request-response)

**Analog:** None in codebase. Pattern from `02-RESEARCH.md` §Standard Stack (CDN tags) and §Pattern 3 (Scalar panel).

**CDN `<head>` block** — all pinned, SRI hashes to be fetched during implementation:
```html
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — Backlot Studio</title>
    <!-- Bootstrap 5.3.8 -->
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css"
          crossorigin="anonymous" />
    <!-- Bootstrap Icons 1.13.1 -->
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css"
          crossorigin="anonymous" />
    <!-- Scalar API Reference 1.60.0 — must be in <head> so Scalar global is available before turbo:load -->
    <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference@1.60.0/dist/browser/standalone.min.js"
            crossorigin="anonymous"></script>
    <link rel="stylesheet" href="~/css/studio.css" />
</head>
```

**Two-panel shell structure** (Bootstrap flex, Phase 1 D-01):
```html
<body>
<div class="d-flex" style="min-height:100vh">
    <aside id="sidebar" data-turbo-permanent style="width:240px; flex-shrink:0">
        <partial name="_Sidebar" />
    </aside>
    <main class="flex-grow-1 p-4">
        @RenderBody()
    </main>
</div>

<!-- Scalar side panel (data-turbo-permanent: persists across Turbo Drive navigations) -->
<div id="scalar-panel" data-turbo-permanent
     style="position:fixed; top:0; right:0; width:min(480px,100vw); height:100vh; z-index:1055;
            transform:translateX(100%); transition:transform .25s ease;">
    <button class="btn-close position-absolute top-0 end-0 m-2"
            aria-label="Close API Reference panel"
            onclick="closeScalarPanel()">
    </button>
    <div id="scalar-mount"></div>
</div>
<div id="scalar-backdrop" data-turbo-permanent
     style="display:none; position:fixed; inset:0; background:rgba(0,0,0,.4); z-index:1054;"
     onclick="closeScalarPanel()">
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

---

### `wwwroot/js/studio.js` (client utility, event-driven)

**Analog:** None in codebase. Pattern from `02-RESEARCH.md` §Pattern 3 (Scalar init/teardown).

**Critical note:** Use `turbo:load` (not `DOMContentLoaded`) for all init. `data-turbo-permanent` requires a stable `id`. Check sentinel before calling `createApiReference()`.

```javascript
// wwwroot/js/studio.js
// Scalar init — runs on every Turbo navigation but initializes only once.
document.addEventListener('turbo:load', function () {
    const panel = document.getElementById('scalar-panel');
    if (!panel || panel.dataset.scalarInitialized) return;

    // Guard: typeof check in case CDN loads slowly
    if (typeof Scalar === 'undefined') return;

    Scalar.createApiReference('#scalar-mount', {
        url: '/openapidoc.json',
        darkMode: false,
        defaultOpenAllTags: false,
    });
    panel.dataset.scalarInitialized = 'true';
});

// Close panel before Turbo Drive navigates away (resets open state only)
document.addEventListener('turbo:before-visit', function () {
    const panel = document.getElementById('scalar-panel');
    if (panel) panel.classList.remove('is-open');
    const backdrop = document.getElementById('scalar-backdrop');
    if (backdrop) backdrop.style.display = 'none';
});

function openScalarPanel(endpointPath) {
    const panel = document.getElementById('scalar-panel');
    const backdrop = document.getElementById('scalar-backdrop');
    if (!panel) return;
    panel.classList.add('is-open');
    backdrop.style.display = 'block';
    panel.focus();
    // v1: open at top level; hash deep-linking deferred (hash format is Scalar-version-internal)
}

function closeScalarPanel() {
    const panel = document.getElementById('scalar-panel');
    const backdrop = document.getElementById('scalar-backdrop');
    if (panel) panel.classList.remove('is-open');
    if (backdrop) backdrop.style.display = 'none';
}

// Escape key closes the panel
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeScalarPanel();
});

// Sidebar collapse toggle (Phase 1 — placeholder; expand in Plan 01-01)
document.addEventListener('turbo:load', function () {
    // Sidebar state is preserved by data-turbo-permanent on <aside id="sidebar">
});
```

---

### `wwwroot/css/studio.css` (stylesheet)

**Analog:** None. Pattern from `02-RESEARCH.md` §Pattern 3 and `02-UI-SPEC.md` §Spacing/Color.

```css
/* wwwroot/css/studio.css */

/* Scalar panel open state */
#scalar-panel.is-open {
    transform: translateX(0);
}

/* Sidebar collapsed state (icon-rail) */
aside#sidebar.collapsed {
    width: 64px;
}

/* Scenario category heading */
.scenario-category-heading {
    font-size: .875rem;   /* 14px */
    font-weight: 600;
    letter-spacing: .05em;
    text-transform: uppercase;
}
```

---

### `Pages/Shared/_Sidebar.cshtml` (partial view, request-response)

**Analog:** None. Pattern from Phase 1 CONTEXT.md decisions D-01, D-02, D-03, D-08.

**Active nav pattern** — the view sets `ViewData["ActiveNav"]` on each page; the sidebar reads it:
```html
@{
    var activeNav = ViewData["ActiveNav"] as string ?? "";
}
<nav class="d-flex flex-column h-100 p-2" style="background: var(--bs-tertiary-bg)">
    <div class="mb-auto">
        <a href="/scenarios"
           class="nav-link @(activeNav == "scenarios" ? "active fw-semibold text-primary" : "text-muted")"
           data-turbo-action="advance">
            <i class="bi bi-list-task me-2"></i> Scenarios
        </a>
        <a href="/roles"
           class="nav-link @(activeNav == "roles" ? "active fw-semibold text-primary" : "text-muted disabled")"
           aria-disabled="true">
            <i class="bi bi-person-bounding-box me-2"></i> Roles
        </a>
    </div>
    <div class="border-top pt-2 mt-2">
        <small class="text-muted d-block">@ViewData["Username"]</small>
        <a href="/logout" class="nav-link text-muted small">Logout</a>
    </div>
</nav>
```

---

## Shared Patterns

### Authentication Guard
**Apply to:** All PageModel classes except `Pages/Account/Login.cshtml.cs`
```csharp
[Authorize]
public class IndexModel : PageModel { ... }
```
The `[Authorize]` attribute relies on cookie auth configured in `Program.cs` (Phase 1 artifact). When the cookie is absent or expired, ASP.NET Core redirects to `/Login` automatically.

### API Error Handling
**Apply to:** All `OnGetAsync()` / `OnPostAsync()` methods that call `BacklotApiClient`
```csharp
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    ErrorMessage = "Could not load [resource]. Check that the Backlot API is reachable and that your credentials are valid.";
}
// Do NOT catch UnauthorizedAccessException — let it propagate to the 401 handler
```

### ViewData Active Nav Convention
**Apply to:** All page `.cshtml` files
```csharp
// In each page's @{ } block:
ViewData["Title"] = "Page Title";
ViewData["ActiveNav"] = "nav-key";  // e.g., "scenarios", "roles"
```
The sidebar reads `ViewData["ActiveNav"]` to apply `.active` class to the matching nav link.

### System.Text.Json Deserialization
**Apply to:** All `BacklotApiClient` methods
```csharp
// No JsonSerializerOptions needed — API uses PascalCase properties
// and System.Text.Json defaults match (case-insensitive by default in ReadFromJsonAsync)
var envelope = await response.Content
    .ReadFromJsonAsync<ApiEnvelope<T>>();
```

---

## No Analog Found

All files in Phase 2 are first-instance patterns. The following files have no codebase analog and must be implemented entirely from RESEARCH.md patterns and ASP.NET Core Razor Pages defaults:

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `Pages/Scenarios/Index.cshtml.cs` | page model | request-response | No Razor Pages exist in codebase |
| `Pages/Scenarios/Index.cshtml` | view | request-response | No Razor Pages exist in codebase |
| `Pages/Shared/_Layout.cshtml` | layout | request-response | No Razor Pages exist in codebase |
| `Pages/Shared/_Sidebar.cshtml` | partial | request-response | No Razor Pages exist in codebase |
| `Services/BacklotApiClient.cs` | service | request-response | No typed HTTP client exists in Studio |
| `wwwroot/js/studio.js` | utility | event-driven | No JS files exist in Studio |
| `wwwroot/css/studio.css` | stylesheet | — | No CSS files exist in Studio |

**The `ApiEnvelope<T>` and `ScenarioItem` DTO shapes ARE verified against real Backlot source** (`Backlot.Http/Media/Formatters/JsonResponse.cs` lines 1–17 and `Backlot.Defaults/Scenarios/Configuration/Models/ScenarioResultItem.cs` as cited in RESEARCH.md).

---

## Metadata

**Analog search scope:** `/home/jeroen/Projects/Backlot/Backlot.Studio/`, `/home/jeroen/Projects/Backlot/Backlot.Demo.Web/`, `/home/jeroen/Projects/Backlot/Backlot.WebApp/`, `/home/jeroen/Projects/Backlot/Backlot.Http/`
**Files scanned:** 30+
**Razor Pages found:** 0
**Pattern extraction date:** 2026-06-21
**Primary pattern sources:** `02-RESEARCH.md` (HIGH confidence), `Backlot.Http/Media/Formatters/JsonResponse.cs` (verified source)
