---
phase: quick-260626-rk0
plan: 01
subsystem: api
tags: [httpclient, refactor, razor-pages, system-text-json]

requires:
  - phase: quick-260626-ou7
    provides: generic PlayAsync / PlayAllowingClientErrorAsync primitives on BacklotApiClient
provides:
  - "All 7 typed call sites rewired onto the generic Play* primitives"
  - "7 typed wrapper methods removed from BacklotApiClient + IBacklotApiClient"
  - "UnwrapRoleDetail promoted to public for direct detail-site unwrapping"
affects: [BacklotApiClient, role-detail, role-edit]

tech-stack:
  added: []
  patterns:
    - "One convention-based primitive set (Play*) is the single API-client surface; PageModels call it directly and read .Body off ApiEnvelope<T>"

key-files:
  created: []
  modified:
    - Backlot.Studio/Pages/Scenarios/Index.cshtml.cs
    - Backlot.Studio/Pages/Roles/Index.cshtml.cs
    - Backlot.Studio/Pages/Roles/Relations.cshtml.cs
    - Backlot.Studio/Pages/Roles/Detail.cshtml.cs
    - Backlot.Studio/Pages/Roles/Edit.cshtml.cs
    - Backlot.Studio/Services/BacklotApiClient.cs
    - Backlot.Studio/Services/IBacklotApiClient.cs

key-decisions:
  - "Detail sites unwrap inline via the now-public static BacklotApiClient.UnwrapRoleDetail(env.Body) instead of a typed wrapper"
  - "Added `using System.Text.Json;` to Edit.cshtml.cs so PlayAsync<JsonElement> and the local JsonElement? detail compile cleanly"

patterns-established:
  - "PageModels read ?.Body off the ApiEnvelope<T> returned by Play* rather than calling typed wrappers"

requirements-completed: []

duration: 2min
completed: 2026-06-26
status: complete
---

# Phase quick-260626-rk0: Inline API Client Call Sites Summary

**All 7 typed BacklotApiClient wrappers collapsed onto the generic PlayAsync / PlayAllowingClientErrorAsync primitives; wrappers deleted from impl + interface, build clean with 0 warnings.**

## Performance

- **Duration:** 2 min
- **Started:** 2026-06-26T17:53:43Z
- **Completed:** 2026-06-26T17:56:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Rewired 5 PageModels (Scenarios/Index, Roles/Index, Roles/Relations, Roles/Detail, Roles/Edit — 9 call sites total including Edit's 4) to call Play* directly and read `?.Body`
- Detail/Edit unwrap role detail inline via the now-public `BacklotApiClient.UnwrapRoleDetail(env.Body)`
- Deleted the 7 typed wrappers (GetScenariosAsync, FindRolesAsync, GetRoleDetailAsync, GetRoleRelationsAsync, GetRoleSchemaAsync, ValidateRoleAsync, PersistRoleAsync) from both `BacklotApiClient.cs` and `IBacklotApiClient.cs`
- HTTP path tokens (`director/scenarios`, `director/roles`, `simplequery/find`, `persist/relations`, `persist/persist`, `role/isvalid`, `seekbase/detail` + `For`/`Uid` payload shapes) preserved byte-for-byte

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewire all 7 call sites to the generic primitives** - `ac9f2c0` (refactor)
2. **Task 2: Make UnwrapRoleDetail public, delete 7 wrappers from impl + interface** - `e8629be` (refactor)

## Files Created/Modified
- `Backlot.Studio/Pages/Scenarios/Index.cshtml.cs` - GetScenariosAsync → PlayAsync<IEnumerable<ScenarioItem>>("director","scenarios"); reads result?.Body
- `Backlot.Studio/Pages/Roles/Index.cshtml.cs` - FindRolesAsync → PlayAsync<FindResult>("simplequery","find",request); RoleResult = result?.Body
- `Backlot.Studio/Pages/Roles/Relations.cshtml.cs` - GetRoleRelationsAsync → PlayAsync<IEnumerable<RelationItem>>("persist","relations", new { Uid }); reads result?.Body
- `Backlot.Studio/Pages/Roles/Detail.cshtml.cs` - GetRoleDetailAsync → PlayAsync<JsonElement>("seekbase","detail", new { For }) + UnwrapRoleDetail(env.Body)
- `Backlot.Studio/Pages/Roles/Edit.cshtml.cs` - 4 sites rewired (detail+schema on GET, schema+detail+isvalid+persist on POST); added `using System.Text.Json;`
- `Backlot.Studio/Services/BacklotApiClient.cs` - UnwrapRoleDetail made public; 7 wrapper methods + doc comments removed
- `Backlot.Studio/Services/IBacklotApiClient.cs` - 7 interface members removed; Play* primitives + IsAuthenticatedAsync/WhoAmIAsync retained

## Decisions Made
- Added `using System.Text.Json;` to Edit.cshtml.cs (the plan permitted this only if a build error required it; the local `JsonElement? detail` and `PlayAsync<JsonElement>` needed it). Resulting build is clean with 0 warnings.

## Deviations from Plan

None - plan executed exactly as written. (The `using System.Text.Json;` addition in Edit.cshtml.cs was explicitly sanctioned by the plan's Task 1 note.)

## Issues Encountered

**Observation (not caused by this task):** The session-start git snapshot listed pre-existing uncommitted changes to `Backlot.Studio/Services/ApiEnvelope.cs` (staged) and `Backlot.sln` (unstaged). By the time the first task commit was made, those entries no longer appeared in the index, and the working tree showed no diff vs HEAD for either file. Both task commits were verified via `git show --stat` to contain ONLY the 7 intended files — neither `ApiEnvelope.cs` nor `Backlot.sln` was staged or committed here. The disappearance occurred outside this executor's git operations (the start-of-session snapshot predated the orchestrator's own plan-doc commit).

## User Setup Required

None - no external service configuration required.

## Verification

- `dotnet build Backlot.Studio/Backlot.Studio.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)**
- grep across `Backlot.Studio/**/*.cs` for each of the 7 deleted method names → **0 references each**
- `UnwrapRoleDetail` confirmed `public static` at BacklotApiClient.cs:119
- IsAuthenticatedAsync, WhoAmIAsync, and the three Play* primitives remain on both impl and interface

## Next Phase Readiness
- BacklotApiClient surface is now a single convention-based primitive set (Play* + the two auth helpers + public UnwrapRoleDetail). No follow-up blockers.

## Self-Check: PASSED

- Commit `ac9f2c0` (Task 1) — FOUND
- Commit `e8629be` (Task 2) — FOUND
- SUMMARY.md — FOUND
- 7 modified source files present and committed

---
*Phase: quick-260626-rk0*
*Completed: 2026-06-26*
