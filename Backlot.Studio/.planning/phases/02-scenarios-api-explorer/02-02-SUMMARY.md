---
phase: 02-scenarios-api-explorer
plan: "02"
subsystem: ui
tags: [scalar, turbo, cdn, static-asset, openapi, side-panel]

requires:
  - phase: 02-scenarios-api-explorer
    plan: "01"
    provides: openScalarPanel() call site on scenario card buttons in Pages/Scenarios/Index.cshtml

provides:
  - Static asset route /openapidoc.json (wwwroot/openapidoc.json served by UseStaticFiles)
  - DOM elements scalar-panel + scalar-backdrop (data-turbo-permanent) + scalar-mount in _Layout.cshtml
  - JS functions openScalarPanel(endpointPath) and closeScalarPanel() in wwwroot/js/studio.js
  - Single-init Scalar bootstrap on turbo:load guarded by panel.dataset.scalarInitialized
  - turbo:before-visit listener resetting open state on navigation
  - CSS rule #scalar-panel.is-open driving slide-in transition in wwwroot/css/studio.css

affects:
  - All pages in the layout (scalar panel + backdrop are global layout elements)

tech-stack:
  added:
    - "@scalar/api-reference@1.60.0 (CDN, pinned, SRI sha384-4BdmZQQTc462+ocGPo+GP3Hi/eQjMQTmNkSU9J5w3FD6hGUEmU2PqNRnbklONt4R)"
  patterns:
    - data-turbo-permanent on scalar-panel/scalar-backdrop for Turbo Drive persistence
    - panel.dataset.scalarInitialized sentinel prevents double-mount across navigations
    - typeof Scalar guard defends against slow CDN load
    - Scalar.createApiReference('#scalar-mount', {url: '/openapidoc.json'}) single-init pattern
    - turbo:before-visit resets open state (does not destroy Scalar instance)
    - CSS transform-based slide-in (translateX(100%) base, translateX(0) open) via is-open class

key-files:
  created:
    - wwwroot/openapidoc.json
  modified:
    - Pages/Shared/_Layout.cshtml
    - wwwroot/js/studio.js
    - wwwroot/css/studio.css

key-decisions:
  - "Scalar CDN pinned to 1.60.0 with sha384 SRI integrity hash — computed from live CDN file to satisfy T-02-04 supply-chain mitigation"
  - "openapidoc.json moved to wwwroot/ (not custom PhysicalFileProvider on content root) — avoids exposing appsettings; served cleanly by UseStaticFiles"
  - "background:#fff added to scalar-panel inline style — prevents transparent panel obscuring page content"
  - "Hash deep-linking deferred for v1 — endpointPath parameter accepted but unused; Scalar hash format is version-internal per RESEARCH Pitfall 4"

requirements-completed: ["SCEN-02"]

duration: 3min
completed: 2026-06-22
status: complete
---

# Phase 02 Plan 02: Scalar API Reference Side Panel Summary

**Pinned Scalar 1.60.0 CDN bundle mounted once in a data-turbo-permanent slide-in panel, served alongside a static openapidoc.json, with open/close/Escape JS and CSS transition driven by the is-open class**

## Performance

- **Duration:** 3 min
- **Started:** 2026-06-22T09:14:00Z
- **Completed:** 2026-06-22T09:17:22Z
- **Tasks:** 2
- **Files modified:** 4 (1 created in wwwroot, 3 modified)

## Accomplishments

- Moved `openapidoc.json` from the project root to `wwwroot/openapidoc.json` so `UseStaticFiles()` serves it at `/openapidoc.json` without any custom file provider
- Added pinned `@scalar/api-reference@1.60.0` CDN script tag in `<head>` with sha384 SRI integrity hash (`sha384-4BdmZQQTc462+ocGPo+GP3Hi/eQjMQTmNkSU9J5w3FD6hGUEmU2PqNRnbklONt4R`) and `crossorigin="anonymous"`, satisfying threat T-02-04
- Added `data-turbo-permanent` `scalar-panel` div (fixed 480px right, z-index 1055, slide-in transform, close button, `scalar-mount`) and sibling `scalar-backdrop` div to `_Layout.cshtml`
- Added single-init `turbo:load` listener in `studio.js` using `panel.dataset.scalarInitialized` sentinel to prevent double-mounting Scalar across Turbo Drive navigations
- Added `typeof Scalar` guard for slow CDN loads, `openScalarPanel`/`closeScalarPanel` functions, Escape keydown handler, and `turbo:before-visit` open-state reset
- Added `#scalar-panel.is-open { transform: translateX(0); }` CSS rule driving the slide-in transition

## Task Commits

Each task was committed atomically:

1. **Task 1: Serve openapidoc.json from wwwroot and add Scalar CDN + permanent panel markup** - `ccc2a6b` (feat)
2. **Task 2: Add single-init Scalar bootstrap, panel open/close/Escape JS, and slide-in CSS** - `3856798` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `wwwroot/openapidoc.json` — OpenAPI spec moved from project root; served at `/openapidoc.json` by static files middleware
- `Pages/Shared/_Layout.cshtml` — Added Scalar CDN `<script>` in `<head>` with SRI; added `#scalar-panel` and `#scalar-backdrop` permanent divs after the main shell
- `wwwroot/js/studio.js` — Added Scalar single-init turbo:load listener, turbo:before-visit reset, openScalarPanel/closeScalarPanel, Escape handler
- `wwwroot/css/studio.css` — Added `#scalar-panel.is-open { transform: translateX(0); }` slide-in rule

## Decisions Made

- **Computed SRI hash from live CDN download** — `openssl dgst -sha384 -binary | base64` applied to the pinned 1.60.0 standalone.min.js file; ensures tamper-detection per T-02-04
- **background:#fff on scalar-panel** — Added during implementation to prevent the panel being visually transparent over page content; minor correctness addition not in plan but required for correct UX
- **Hash deep-linking deferred** — `openScalarPanel` accepts `endpointPath` but does not use it; Scalar's internal hash slug format is version-specific (RESEARCH Pitfall 4) and deferred for v1

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing critical functionality] Added background color to scalar-panel**
- **Found during:** Task 1 implementation review
- **Issue:** The panel markup in _Layout.cshtml had no background color specified beyond what the plan described; without it the panel would be transparent over page content
- **Fix:** Added `background:#fff` to the panel's inline style
- **Files modified:** Pages/Shared/_Layout.cshtml
- **Commit:** ccc2a6b

## Threat Surface Scan

No new security-relevant surface beyond what the plan's threat model covers. All mitigations applied:
- T-02-04: SRI hash computed and pinned; crossorigin="anonymous" applied
- T-02-05: openapidoc.json serves only the public API spec from wwwroot; no appsettings exposure
- T-02-06: Single-init sentinel applied; double-mount prevented

## Known Stubs

None — the panel wires directly to `/openapidoc.json` and `Scalar.createApiReference` with a real data source.

## Self-Check: PASSED

- wwwroot/openapidoc.json: FOUND
- Pages/Shared/_Layout.cshtml: FOUND with scalar-panel, scalar-backdrop, Scalar CDN 1.60.0, SRI hash
- wwwroot/js/studio.js: FOUND with createApiReference, scalarInitialized, openScalarPanel, closeScalarPanel, turbo:load, turbo:before-visit, Escape
- wwwroot/css/studio.css: FOUND with #scalar-panel.is-open rule
- Task 1 commit ccc2a6b: present in git log
- Task 2 commit 3856798: present in git log

---
*Phase: 02-scenarios-api-explorer*
*Completed: 2026-06-22*
