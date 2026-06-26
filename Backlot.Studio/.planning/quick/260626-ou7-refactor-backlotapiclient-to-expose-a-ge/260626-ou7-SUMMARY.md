---
phase: quick-260626-ou7
plan: 01
subsystem: api
tags: [httpclient, refactor, generics, backlot-api, validation]

# Dependency graph
requires:
  - phase: 04-role-editing
    provides: BacklotApiClient typed methods + ValidationOutcome 4xx recovery + UnwrapRoleDetail
provides:
  - "Public PlayAsync<T> GET/POST primitives mirroring api/role/{rolename}/{scenario}"
  - "Public PlayAllowingClientErrorAsync<T> primitive carrying generalized 4xx structured-body recovery"
  - "Typed IBacklotApiClient methods reduced to thin wrappers over the primitives"
affects: [backlot-studio, api-client, role-editing]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Generic route-convention primitive (PlayAsync<T>) replacing per-method path/envelope boilerplate"
    - "Error-tolerant POST primitive (PlayAllowingClientErrorAsync) recovering structured 4xx bodies"

key-files:
  created: []
  modified:
    - Backlot.Studio/Services/IBacklotApiClient.cs
    - Backlot.Studio/Services/BacklotApiClient.cs

key-decisions:
  - "PlayAllowingClientErrorAsync returns the envelope (not body) so callers keep status/timing metadata; null-Body 4xx falls through to EnsureSuccessAsync"
  - "Used `is { Body: { } }` pattern instead of `?.Body is not null` to avoid CS8978 on the unconstrained generic T"

patterns-established:
  - "Pattern 1: Typed API methods delegate to PlayAsync<T>(roleName, scenario, ...) rather than building literal paths"
  - "Pattern 2: GET uid is appended only when non-empty and escaped via Uri.EscapeDataString (T-ou7-01)"

requirements-completed: [WR-02, WR-04, WR-05]

# Metrics
duration: 2min
completed: 2026-06-26
status: complete
---

# Phase quick-260626-ou7: Refactor BacklotApiClient to expose a generic Play primitive Summary

**BacklotApiClient now exposes public `PlayAsync<T>` GET/POST primitives plus `PlayAllowingClientErrorAsync<T>` mirroring the `api/role/{rolename}/{scenario}` convention 1:1, with all typed methods reduced to thin wrappers and identical wire requests.**

## Performance

- **Duration:** 2 min
- **Started:** 2026-06-26T15:58:01Z
- **Completed:** 2026-06-26T16:00:13Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Added three public members to `IBacklotApiClient`: `PlayAsync<T>` (GET overload), `PlayAsync<T>` (POST overload), and `PlayAllowingClientErrorAsync<T>`
- Implemented the primitives in `BacklotApiClient` using the shared `JsonOptions` + `EnsureSuccessAsync`, with the GET path appending `?uid=` only for non-empty uid via `Uri.EscapeDataString`
- Generalized the inline `ValidateRoleAsync` 4xx structured-body recovery into `PlayAllowingClientErrorAsync<T>` (401/403 + 5xx still throw; structured 4xx recovered — WR-02)
- Rewired all nine typed methods to delegate to the primitives and deleted the now-unused private `GetEnvelopeAsync` / `PostEnvelopeAsync` helpers
- Preserved `UnwrapRoleDetail` post-step (WR-03) and the WR-04/WR-05 comment intent; HTTP requests remain byte-for-byte equivalent

## Task Commits

Each task was committed atomically:

1. **Task 1: Add PlayAsync + PlayAllowingClientErrorAsync primitives to interface and client** - `0d6352f` (feat)
2. **Task 2: Rewire typed methods to thin wrappers and remove the old private helpers** - `8d68433` (refactor)

## Files Created/Modified
- `Backlot.Studio/Services/IBacklotApiClient.cs` - Added PlayAsync (GET + POST) and PlayAllowingClientErrorAsync interface members
- `Backlot.Studio/Services/BacklotApiClient.cs` - Added generic primitive implementations, rewired typed methods to delegate to them, removed GetEnvelopeAsync/PostEnvelopeAsync

## Decisions Made
- `PlayAllowingClientErrorAsync<T>` returns the full `ApiEnvelope<T>` rather than the unwrapped body, keeping the wrapper consistent with `PlayAsync` and letting `ValidateRoleAsync` unwrap with `envelope?.Body`.
- A null-`Body` 4xx response intentionally falls through to `EnsureSuccessAsync` (rich throw) exactly as the original `ValidateRoleAsync` did.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Adjusted null-check to avoid CS8978 on unconstrained generic**
- **Found during:** Task 1 (Add primitives)
- **Issue:** `if (failEnvelope?.Body is not null)` failed to compile — `'T' cannot be made nullable` (CS8978) because `Body` is `T?` over an unconstrained generic and `?.` re-nullifies it.
- **Fix:** Changed to `if (failEnvelope is { Body: { } })`, matching the original code's `is { } body` pattern style.
- **Files modified:** Backlot.Studio/Services/BacklotApiClient.cs
- **Verification:** `dotnet build Backlot.Studio/Backlot.Studio.csproj` succeeds (0 warnings, 0 errors).
- **Committed in:** `0d6352f` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Compile-level adjustment only; no behavior change. No scope creep.

## Issues Encountered
None beyond the CS8978 compile fix noted above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Refactor complete; the client now mirrors the server route convention 1:1 with a single generic primitive.
- All existing callers (Pages/Roles, Pages/Scenarios, Detail/Edit pages) compile unchanged against the same `IBacklotApiClient` signatures.
- Pre-existing unrelated working-tree changes (`Services/ApiEnvelope.cs`, `Backlot.sln`) were intentionally left unstaged.

## Self-Check: PASSED
- FOUND: Backlot.Studio/Services/IBacklotApiClient.cs
- FOUND: Backlot.Studio/Services/BacklotApiClient.cs
- FOUND commit: 0d6352f
- FOUND commit: 8d68433

---
*Phase: quick-260626-ou7*
*Completed: 2026-06-26*
</content>
</invoke>
