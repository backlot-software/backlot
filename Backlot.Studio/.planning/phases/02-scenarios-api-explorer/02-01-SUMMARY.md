---
phase: 02-scenarios-api-explorer
plan: "01"
subsystem: ui
tags: [razor-pages, dto, http-client, bootstrap, turbo, scenarios]

requires:
  - phase: 01-foundation-auth
    provides: BacklotApiClient typed HTTP client, IBacklotApiClient interface, ApiEnvelope<T> DTO, BasicAuthHandler, cookie auth, session, _Sidebar.cshtml

provides:
  - ScenarioItem DTO (Models/Api/ScenarioItem.cs) for deserializing scenario list API responses
  - IBacklotApiClient.GetScenariosAsync() and BacklotApiClient implementation
  - Pages/Scenarios/Index route — grouped scenario list with success/empty/error states
  - Sidebar "Scenarios" nav link activated via ViewData["ActiveNav"]

affects:
  - 02-02 (Scalar side panel — openScalarPanel() invoked by scenario card buttons created here)

tech-stack:
  added: []
  patterns:
    - Fetch-unwrap-render via existing GetEnvelopeAsync<T> helper — no per-method envelope deserialization
    - LINQ GroupBy + Select tuple projection for category grouping in PageModel
    - ViewData["ActiveNav"] convention for sidebar active-link state
    - Exception filter catch (HttpRequestException or TaskCanceledException) for API error isolation

key-files:
  created:
    - Models/Api/ScenarioItem.cs
    - Pages/Scenarios/Index.cshtml.cs
    - Pages/Scenarios/Index.cshtml
  modified:
    - Services/IBacklotApiClient.cs
    - Services/BacklotApiClient.cs
    - Pages/Shared/_Sidebar.cshtml

key-decisions:
  - "Reused ApiEnvelope<T> from Services/ (Phase 1 artifact) instead of duplicating in Models/Api/ — plan's reconcile clause applied"
  - "Injected IBacklotApiClient (not concrete BacklotApiClient) in IndexModel — DI is registered on the interface so concrete injection would fail"
  - "Scenarios nav link uses data-turbo-action=advance for browser history support"

patterns-established:
  - "Scenarios page: fetch via IBacklotApiClient, group by first tag, render three states (error/empty/success)"

requirements-completed: ["SCEN-01"]

duration: 2min
completed: 2026-06-22
status: complete
---

# Phase 02 Plan 01: Scenarios Overview Page Summary

**Server-rendered /scenarios page delivering grouped scenario cards with fetch-unwrap-render via BacklotApiClient, three view states (success/empty/error), and sidebar active-nav activation**

## Performance

- **Duration:** 2 min
- **Started:** 2026-06-22T08:25:52Z
- **Completed:** 2026-06-22T08:27:31Z
- **Tasks:** 2
- **Files modified:** 5 (3 created, 3 modified)

## Accomplishments
- Added `ScenarioItem` DTO in `Models/Api/` with all required properties initialized to empty arrays
- Extended `IBacklotApiClient` and `BacklotApiClient` with `GetScenariosAsync()` that reuses the existing `GetEnvelopeAsync<T>` helper
- Built `Pages/Scenarios/IndexModel` with `[Authorize]` guard, tag-based grouping, and scoped error handling
- Created `Pages/Scenarios/Index.cshtml` with all three states: grouped card list, empty state, and danger-alert error state
- Activated "Scenarios" sidebar nav link via `ViewData["ActiveNav"]` convention

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ScenarioItem DTO and GetScenariosAsync client method** - `98f8444` (feat)
2. **Task 2: Build Scenarios Index PageModel and view; activate sidebar nav** - `22801a2` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `Models/Api/ScenarioItem.cs` - DTO with Scenario, Result, Roles, Tags, Endpoints, Configurations (arrays defaulted to [])
- `Services/IBacklotApiClient.cs` - Added GetScenariosAsync() signature
- `Services/BacklotApiClient.cs` - Implemented GetScenariosAsync() via existing GetEnvelopeAsync<T> helper
- `Pages/Scenarios/Index.cshtml.cs` - [Authorize] IndexModel with tag-based grouping and error handling
- `Pages/Scenarios/Index.cshtml` - Three-state view: error alert, empty state, grouped scenario cards
- `Pages/Shared/_Sidebar.cshtml` - Scenarios nav link activated; reads ViewData["ActiveNav"]

## Decisions Made
- **Reused `ApiEnvelope<T>` from `Services/`** — Phase 1 created this class with identical properties; the plan's reconcile clause was applied, no duplicate created in `Models/Api/`
- **Injected `IBacklotApiClient` (not concrete)** — DI is registered as `AddHttpClient<IBacklotApiClient, BacklotApiClient>`, so the concrete type is not directly resolvable; used interface to match Phase 1 convention
- **`data-turbo-action="advance"` on Scenarios link** — enables proper browser history navigation via Turbo Drive

## Deviations from Plan

None — plan executed exactly as written, with one reconcile clause applied as specified: `ApiEnvelope<T>` existed in `Services/` from Phase 1, so `Models/Api/ApiEnvelope.cs` was not created (as instructed).

## Issues Encountered
None.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Plan 02-01 complete: `/scenarios` route renders grouped scenario cards backed by the Backlot API
- Plan 02-02 can now define `openScalarPanel()` (invoked by card buttons here) and the Scalar side panel markup
- No blockers

---
*Phase: 02-scenarios-api-explorer*
*Completed: 2026-06-22*
