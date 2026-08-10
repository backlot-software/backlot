---
phase: 01-foundation-auth
plan: "01"
subsystem: shell
tags: [scaffold, layout, bootstrap, turbo, sidebar]
status: complete

dependency_graph:
  requires: []
  provides:
    - Backlot.Studio.csproj (net10.0 Razor Pages project)
    - Program.cs (DI stubs for Plans 01-02/01-03)
    - _Layout.cshtml (authenticated two-panel shell)
    - _LoginLayout.cshtml (minimal login layout)
    - _Sidebar.cshtml (sidebar partial with placeholder nav)
    - studio.css (sidebar collapse styles)
    - studio.js (sidebar toggle with turbo:load)
  affects:
    - Backlot.sln (project added)

tech_stack:
  added:
    - ASP.NET Core 10 Razor Pages (net10.0)
    - Bootstrap 5.3.8 (CDN, SRI-pinned)
    - Bootstrap Icons 1.13.1 (CDN, SRI-pinned)
    - Hotwired Turbo 8.0.23 (CDN, SRI-pinned, ESM module)
  patterns:
    - two-panel Bootstrap flex layout (240px fixed sidebar + flex-grow-1 main)
    - data-turbo-permanent sidebar for cross-navigation state preservation
    - turbo:load event listener pattern (NOT DOMContentLoaded)
    - CSS :has() selector for margin-left adjustment on sidebar collapse

key_files:
  created:
    - Backlot.Studio/Backlot.Studio.csproj
    - Backlot.Studio/Program.cs
    - Backlot.Studio/appsettings.json
    - Backlot.Studio/Pages/Shared/_Layout.cshtml
    - Backlot.Studio/Pages/Shared/_LoginLayout.cshtml
    - Backlot.Studio/Pages/Shared/_Sidebar.cshtml
    - Backlot.Studio/Pages/_ViewImports.cshtml
    - Backlot.Studio/Pages/_ViewStart.cshtml
    - Backlot.Studio/wwwroot/css/studio.css
    - Backlot.Studio/wwwroot/js/studio.js
  modified:
    - Backlot.sln (Backlot.Studio project added)

decisions:
  - "Razor @@ escape for CDN URL containing @hotwired/turbo@8.0.23 — required by Razor parser; renders correctly at runtime"
  - "sidebar-toggle click handler uses element.onclick assignment inside turbo:load to avoid duplicate listeners across navigations"
  - "CSS :has() selector for main margin-left adjustment — supported in Chromium 105+, Firefox 121+, Safari 15.4+; acceptable for developer tool"

metrics:
  duration_minutes: 4
  completed_date: "2026-06-22"
  tasks_completed: 3
  tasks_total: 3
  files_created: 10
  files_modified: 1
---

# Phase 01 Plan 01: Project Scaffold & Shell Layout Summary

**One-liner:** ASP.NET Core 10 Razor Pages project scaffolded with CDN-pinned Bootstrap 5.3.8 + Turbo 8.0.23 two-panel shell, collapsible sidebar, and SRI-hardened CDN assets.

## What Was Built

Scaffolded the Backlot.Studio project from zero: created the `.csproj` targeting `net10.0`, added it to `Backlot.sln`, wrote a minimal `Program.cs` with placeholder comments for Plans 01-02/01-03, and configured `appsettings.json` with the `BacklotApi:BaseUrl` key.

Created the visual shell: `_Layout.cshtml` implements the two-panel Bootstrap flex layout with a 240px fixed sidebar (`<aside id="sidebar" data-turbo-permanent>`) and `flex-grow-1` main area. `_Sidebar.cshtml` has the "Backlot Studio" header, two disabled placeholder nav items (Scenarios/Roles with Bootstrap Icons), an identity block at the bottom, and a 44px toggle button. `_LoginLayout.cshtml` is a minimal layout with only Bootstrap CSS.

`studio.css` provides the sidebar collapse styles: 200ms width transition, 64px icon-rail width, `.sidebar-label` text hide, and a CSS `:has()` rule to adjust the main content margin. `studio.js` wires the toggle handler on `turbo:load` (not `DOMContentLoaded`) to preserve Turbo Drive compatibility — both files include Phase 2 section markers for clean extension.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1: Scaffold project | `009db20` | feat(01-01): scaffold Backlot.Studio project and add to solution |
| Task 2: Shell layouts | `8cc5e49` | feat(01-01): create Bootstrap+Turbo shell layouts and sidebar partial |
| Task 3: CSS/JS assets | `4e32368` | feat(01-01): create sidebar collapse CSS and JS assets |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Razor @-escaping in CDN URL for Turbo**
- **Found during:** Task 2 (build error after creating _Layout.cshtml)
- **Issue:** The Razor parser tried to interpret `@hotwired/turbo@8.0.23` as a Razor directive, causing `error CS0103: The name 'hotwired' does not exist in the current context`
- **Fix:** Used `@@hotwired/turbo@@8.0.23` in the CDN URL — Razor `@@` escapes to a literal `@` at runtime, so the rendered HTML is correct
- **Files modified:** `Backlot.Studio/Pages/Shared/_Layout.cshtml`
- **Commit:** included in `8cc5e49`

**2. [Rule 2 - Missing critical functionality] sidebar-toggle click handler deduplication**
- **Found during:** Task 3 design review
- **Issue:** The PATTERNS.md example attached a separate `document.getElementById('sidebar-toggle')?.addEventListener('click', ...)` outside the `turbo:load` handler, which would add duplicate listeners on each navigation
- **Fix:** Moved click handler inside `turbo:load` using `toggle.onclick = function()` assignment (idempotent — replaces previous handler on each navigation)
- **Files modified:** `Backlot.Studio/wwwroot/js/studio.js`
- **Commit:** `4e32368`

## Known Stubs

None — this plan creates the visual shell only. `ViewData["Username"]` in `_Sidebar.cshtml` is an intentional forward-compatibility hook (populated by authenticated PageModels in Plan 01-03). The disabled nav items (Scenarios, Roles) are the explicit Phase 1 requirement per D-02.

## Threat Flags

No new threat surface beyond the plan's threat model. T-01-01 (CDN asset tampering) mitigated: all 4 CDN resources (Bootstrap CSS, Bootstrap Icons CSS, Bootstrap JS, Turbo ESM) have `integrity="sha384-..."` SRI attributes with hashes computed from the actual CDN payloads.

## Self-Check: PASSED

Files verified:
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Backlot.Studio.csproj` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Program.cs` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/appsettings.json` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Shared/_Layout.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Shared/_LoginLayout.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/Pages/Shared/_Sidebar.cshtml` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/wwwroot/css/studio.css` — FOUND
- `/home/jeroen/Projects/Backlot/Backlot.Studio/wwwroot/js/studio.js` — FOUND

Commits verified in git log: `009db20`, `8cc5e49`, `4e32368` — FOUND
