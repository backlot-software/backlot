---
phase: 04-role-editing
plan: 03
subsystem: api
tags: [razor-pages, json, system-text-json, permissions, gap-closure]

# Dependency graph
requires:
  - phase: 04-role-editing
    provides: "Detail/Edit PageModels reading __Permission/__Skills/__LastModifiedDate from the API response Body"
provides:
  - "Defensive UnwrapRoleDetail at the GetRoleDetailAsync chokepoint — every consumer reads role fields at the correct level"
  - "Edit button enables for writable roles (CanWrite reads __Permission under the unwrapped Role)"
  - "CanCreate badge removed from the role detail view (always-false, framework never emits the property)"
affects: [role-editing, role-detail, role-edit-form, uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Single-chokepoint defensive unwrap: normalize API response shape once in the API client so PageModels stay unchanged"

key-files:
  created: []
  modified:
    - Services/BacklotApiClient.cs
    - Pages/Roles/Detail.cshtml

key-decisions:
  - "Unwrap the seekbase/detail Role wrapper inside GetRoleDetailAsync rather than in each PageModel — one fix corrects Detail.OnGet, Edit.OnGet, and Edit.OnPost simultaneously, no PageModel or interface change."
  - "Make the unwrap defensive: descend into Role only when Body is an object containing a Role object; non-object and future-flat shapes pass through unchanged."
  - "Clone the role element so it survives parent JsonDocument disposal from ReadFromJsonAsync."
  - "Drop the CanCreate badge in Studio instead of patching Backlot.Core PermissionsValueProvider — a cross-project change for a cosmetic, semantically-meaningless badge on an inspect-existing view is unwarranted."

patterns-established:
  - "Defensive response-shape normalization at the API client boundary keeps view/PageModel code shape-agnostic."

requirements-completed: [EDIT-01, EDIT-02, EDIT-03]

# Metrics
duration: 6 min
completed: 2026-06-24
status: complete
---

# Phase 4 Plan 3: Unwrap seekbase/detail Role Wrapper — Edit-Button Gap Closure Summary

**Defensive Role-wrapper unwrap added at the GetRoleDetailAsync chokepoint so CanWrite reads __Permission at the correct level — the Edit button now enables for writable roles — plus removal of the always-false CanCreate badge from the detail view.**

## Performance

- **Duration:** ~6 min
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- Added `private static JsonElement UnwrapRoleDetail(JsonElement body)` to `BacklotApiClient` and applied it inside `GetRoleDetailAsync`, which now returns the unwrapped role (`Body.Role` when the seekbase/detail wrapper is present, else `Body`).
- Fixed the root cause of the Phase 04 UAT blocker: `DetailModel.GetPermissions` / `GetSkills` / `GetNonSystemFields` / `GetPageTitle` and the Edit page's server-side `CanWrite` gate now read `__Permission`/`__Skills`/`__LastModifiedDate`/data fields at the correct level. `CanWrite` is no longer permanently `false`, so the Edit button renders as an enabled link for writable roles.
- Proved the unwrap behavior with a self-contained scratch harness (WRAPPED / FLAT / NON-OBJECT) using a byte-identical copy of the shipped helper body — all cases PASS, exit 0.
- Removed the always-false `CanCreate` badge from `Pages/Roles/Detail.cshtml`; `Backlot.Core` left untouched.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add defensive Role-wrapper unwrap to GetRoleDetailAsync** — `1dcc5de` (fix)
2. **Task 2: Prove the unwrap logic with a self-contained verification harness** — no repo commit (verification-only; the harness lives entirely under the session scratchpad and is NOT committed; the production change is already in Task 1's commit)
3. **Task 3: Remove the always-false CanCreate badge from the role detail view** — `3c744ae` (fix)

## Files Created/Modified
- `Services/BacklotApiClient.cs` — Added `UnwrapRoleDetail` helper; `GetRoleDetailAsync` returns the unwrapped role element (cloned so it survives parent `JsonDocument` disposal). Signature, interface `IBacklotApiClient`, and all PageModels unchanged.
- `Pages/Roles/Detail.cshtml` — Removed the single `CanCreate` badge span from the `__Permission` table cell; `CanRead` and `CanWrite` badges remain intact.

## Verification Results

### Build (Tasks 1 & 3)
`dotnet build Backlot.Studio/Backlot.Studio.csproj` — **Build succeeded, 0 Warning(s), 0 Error(s)** (both after Task 1 and after Task 3).

### CanCreate removal (Task 3)
`grep -c 'CanCreate' Backlot.Studio/Pages/Roles/Detail.cshtml` → **0**. `Backlot.Core` shows no changes (`git status --short Backlot.Core/` empty).

### Unwrap harness (Task 2)
The harness reproduced the `UnwrapRoleDetail` body byte-identically and also replicated `DetailModel.GetPermissions`' `__Permission → CanWrite == JsonValueKind.True` read. Output:

```
PASS  WRAPPED.topLevelPermission — unwrapped element exposes top-level __Permission
PASS  WRAPPED.skills — __Skills[0] == "Foo"
PASS  WRAPPED.canWrite — GetPermissions.CanWrite == true after unwrap
PASS  FLAT.unchanged — returned element unchanged (top-level __Skills == "Bar")
PASS  FLAT.canWrite — defensive top-level CanWrite == true
PASS  NON-OBJECT.array — array passes through unchanged
PASS  NON-OBJECT.string — string passes through unchanged

ALL CASES PASS
EXIT=0
```

- **WRAPPED** confirms `CanWrite == true` after unwrap — the exact UAT-blocking scenario now resolves.
- **FLAT** confirms the defensive top-level path still yields `CanWrite == true` for a future no-wrapper response shape.
- **NON-OBJECT** confirms arrays and strings pass through unchanged with no throw.

The production `UnwrapRoleDetail` body in `Services/BacklotApiClient.cs` is identical to the harnessed copy. The scratch project lives under the session scratchpad (`.../f2c6f9fe-.../scratchpad/unwrap-check/`), outside the repository, and was NOT staged or committed.

## Decisions Made
- Unwrap at the single API-client chokepoint (`GetRoleDetailAsync`) rather than per-PageModel — one fix corrects all three call sites (`Detail.OnGetAsync`, `Edit.OnGetAsync`, `Edit.OnPostAsync`) with no PageModel/interface change.
- Defensive unwrap (descend into `Role` only when `Body` is an object containing a `Role` object; clone the result) — preserves a future flat response shape and never throws on an unexpected shape (mitigates threat T-04-03-01).
- Drop the `CanCreate` badge in Studio rather than patch the `Backlot.Core` framework (`PermissionsValueProvider` never serializes `CanCreate`) — a cross-project change for a cosmetic, meaningless badge on an inspect-existing view is unwarranted. `CanCreate` remains in `GetPermissions`/the `Permissions` tuple (harmless); only the view badge is removed.

## Deviations from Plan

None - plan executed exactly as written.

The plan's Task 2 verify command referenced a scratchpad path with a stale session id; per the execution instructions this was redirected to the current session's scratchpad. This is an environment path substitution, not a scope or behavior deviation.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- The UAT blocker is removed: the Edit button enables for writable roles, and UAT Tests 1/2/3 of `04-UAT.md` are now **unblocked and testable**.
- Full sign-off of UAT Tests 1/2/3 still requires a running, authenticated Backlot API with valid credentials (carried-forward STATE blocker A1/A4) — the live wrapped-response render, the Edit GET form seeding, and the Edit POST 422/303 paths must be exercised end-to-end against real auth before the tests can be marked passed.

## Self-Check: PASSED

- `Services/BacklotApiClient.cs` — FOUND
- `Pages/Roles/Detail.cshtml` — FOUND
- `.planning/phases/04-role-editing/04-03-SUMMARY.md` — FOUND
- Commit `1dcc5de` (Task 1) — FOUND
- Commit `3c744ae` (Task 3) — FOUND
- Scratch harness under session scratchpad — NOT staged/committed (correct)

---
*Phase: 04-role-editing*
*Completed: 2026-06-24*
