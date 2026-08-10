---
phase: 04-role-editing
plan: 02
subsystem: ui
tags: [razor-pages, turbo, http-303, http-422, mass-assignment, validation-summary, success-banner, defense-in-depth]

# Dependency graph
requires:
  - phase: 04-role-editing
    plan: 01
    provides: "TurboEditPageModel (TurboRedirect 303 / TurboInvalidPage 422), GetRoleSchemaAsync/ValidateRoleAsync/PersistRoleAsync, RoleSchema/ValidationOutcome DTOs, rendered /roles/{uid}/edit form, MatchSchema/BuildPayload/IsReadOnly/IsBool/IsNumeric helpers"
provides:
  - "Production edit save orchestration: schema-driven CoerceByType payload, server-side CanWrite re-check, defensive isvalid->422 summary, persist->303, persist-failure handling"
  - "TempData-free 'Role saved.' success banner on the detail page via ?saved=1 query flag"
affects: [role-editing, verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Type-coercing payload build: CoerceByType parses numeric schema Types to CLR types, raw-string fallback on parse failure so the API surfaces validation errors"
    - "Defense-in-depth permission gate: re-check __Permission.CanWrite server-side before isvalid/persist (T-04-02)"
    - "Defensive validation parse: invalid-but-empty Results surfaces a single generic 'Validation failed.' item"
    - "Query-flag success confirmation (?saved=1) read by [BindProperty(SupportsGet=true)] bool Saved — no TempData (D-08)"

key-files:
  created: []
  modified:
    - "Pages/Roles/Edit.cshtml.cs"
    - "Pages/Roles/Detail.cshtml.cs"
    - "Pages/Roles/Detail.cshtml"

key-decisions:
  - "Edit.cshtml view summary block already existed verbatim from 04-01 (the page+model were authored complete in one pass); Task 1 therefore only modified Edit.cshtml.cs, not the view"
  - "CoerceByType returns the raw string on numeric parse failure so a bad numeric input becomes an isvalid validation error rather than a 500 / form crash"
  - "CanWrite=false short-circuits to a 422 re-render with a permission message rather than silently proceeding to persist"

patterns-established:
  - "CoerceByType: Boolean->raw=='true' (false when key absent), numeric->TryParse to CLR type with raw fallback, else->raw string"
  - "Generic-fallback validation item when isvalid reports invalid with empty/missing Results"

requirements-completed: [EDIT-02, EDIT-03]

# Metrics
duration: 6min
completed: 2026-06-23
status: complete
---

# Phase 04 Plan 02: Hardened Edit Save Path + Success Banner Summary

**Finalized the only v1 mutation flow — a mass-assignment-safe, type-coercing, defense-in-depth save that validates via `role/isvalid` (422 + summary block on failure), persists via `persist/persist` (303 on success), and lands on a TempData-free green "Role saved." banner driven by a `?saved=1` redirect flag.**

## Performance
- **Duration:** ~6 min
- **Completed:** 2026-06-23
- **Tasks:** 2
- **Files modified:** 3 (0 created, 3 modified)

## Accomplishments
- **Task 1** — Replaced the 04-01 minimal save path with production orchestration in `Edit.cshtml.cs`:
  - Added `CoerceByType(string type, string? raw)`: bool fields resolve to `raw == "true"` (false when the unchecked checkbox posts nothing, Pitfall 3); numeric schema Types parse to their matching CLR type (`byte`/`short`/`int`/`long`/their unsigned forms/`decimal`/`double`/`float`), falling back to the raw string on parse failure so the API surfaces a validation error instead of crashing; everything else passes through unchanged.
  - `BuildPayload` now routes every schema field through `CoerceByType`, iterating the SCHEMA field list (never posted keys), skipping `Calculated`/read-only fields, and seeding only `Uid` + schema-known fields (T-04-03 mass-assignment guard).
  - Added a server-side `CanWrite` re-check (T-04-02 defense-in-depth): `CanWrite == false` short-circuits to a 422 re-render with "You don't have permission to edit this role." before any isvalid/persist call.
  - Defensive isvalid parse (RESEARCH Q2): invalid-but-empty `Results` now surfaces a single generic `"Validation failed."` item so the summary block is never blank; null `ErrorMessage` items tolerated by the view.
  - The isvalid->422 / persist->303 / persist-failure-preserves-values ordering from 04-01 is retained intact.
- **Task 2** — TempData-free success banner:
  - `DetailModel` gains `[BindProperty(SupportsGet = true)] public bool Saved`.
  - `Detail.cshtml` renders a dismissible `alert alert-success` "Role saved." banner gated on `Model.Saved`, inside the success branch above the page title.
  - The flag travels in the server-constructed `/roles/{uid}?saved=1` Location set by `TurboRedirect` (T-04-06 open-redirect closed — never user-supplied), survives the 303 natively, and is read on GET (D-08: no TempData).

## Task Commits
1. **Task 1: harden edit save path (CoerceByType, CanWrite recheck, defensive isvalid)** — `4520a2b` (feat)
2. **Task 2: TempData-free 'Role saved.' success banner (?saved=1)** — `7d5d623` (feat)

**Plan metadata:** see final docs commit.

## Files Created/Modified
- `Pages/Roles/Edit.cshtml.cs` — added `CoerceByType`, routed `BuildPayload` through it, added the `CanWrite` server-side gate and the empty-Results generic-fallback validation item.
- `Pages/Roles/Detail.cshtml.cs` — added the `Saved` query-flag bind property.
- `Pages/Roles/Detail.cshtml` — added the `alert-success` "Role saved." banner gated on `Model.Saved`.

> Note: `Pages/Roles/Edit.cshtml` (the validation summary block "Please fix the following before saving:" and the persist-failure `alert-danger` block) was already authored complete in 04-01's single-pass view commit, so it required no change in this plan. All Task 1 view acceptance greps pass against the existing file.

## Decisions Made
- **Numeric parse failure → raw fallback (not exception):** `CoerceByType` returns the unparsed string when a numeric field can't parse, so the round-trip degrades into an `isvalid` validation error (shown in the summary block) rather than a server-side crash. This keeps the form resilient to bad input without bypassing server validation.
- **CanWrite gate returns 422, not a redirect:** a forbidden POST re-renders the form body with a permission message via `TurboInvalidPage()` so Turbo swaps the body in place, consistent with the rest of the invalid-path UX.
- **Edit view unchanged:** because 04-01 shipped the full view (including the D-07 summary block), Task 1 was a pure page-model hardening; the plan's "add the summary block" step was already satisfied.

## Deviations from Plan
None requiring deviation rules — plan executed as written. The only adjustment is that the `Edit.cshtml` summary block already existed from 04-01, so Task 1 touched only the page model (documented above; organizational, not a code deviation).

## Issues Encountered
**Live authenticated Turbo 303/422 round-trip remains runtime-unverified (carried-forward A1/A4 from 04-01).**
- The Backlot demo host (`Backlot.Demo.Web/usersandgroups.json`) stores only a salted password **hash** (`pw` is a base64 digest, no plaintext, no documented test password). Basic Auth could not be satisfied to drive a logged-in browser session, exactly as in 04-01.
- Consequently the live HTTP **303 on valid save** and **422 + summary on invalid** were NOT captured against a running authenticated host, nor was the live `isvalid` property casing (A1) confirmed. The design is HIGH-confidence from framework source (RESEARCH Pattern 1 / Q2) and the endpoints were confirmed wired in 04-01.
- The defensive isvalid parse (generic-fallback on empty `Results`) was **not** exercised against a real role response for the same reason — it remains a safety net pending the authenticated run.

**Recommendation for verification/sign-off:** obtain valid Basic Auth credentials for the running host and exercise the full edit round-trip (invalid → 422 body swap with the real isvalid summary; valid → 303 to `/roles/{uid}?saved=1` with the green banner; unchecked bool → persists as false) to close A1/A3/A4.

## User Setup Required
None — no external service configuration. (To close the deferred live smoke test, valid Basic Auth credentials for the Backlot API host are needed.)

## Next Phase Readiness
- EDIT-02 delivered: pre-save validation errors from `role/isvalid` shown as the top-of-form summary block (D-07 recorded deviation — summary, not inline).
- EDIT-03 delivered: save via `persist/persist`, 303 redirect on success to the detail page with the "Role saved." banner, 422 re-render on validation failure, persist-failure preserves entered values with an error message.
- D-08 honored: no TempData anywhere in the edit or detail pages.
- **Carried-forward blocker:** the live authenticated Turbo 303/422 + antiforgery round-trip (A4) and `isvalid` casing (A1) remain runtime-unverified — needs valid host credentials.

## Known Stubs
None — all paths are wired to live API calls (`GetRoleSchemaAsync`, `GetRoleDetailAsync`, `ValidateRoleAsync`, `PersistRoleAsync`); no hardcoded empty/placeholder data flows to the UI.

## Self-Check: PASSED
All 3 modified files present; both task commits (`4520a2b`, `7d5d623`) found in git log. Build succeeds, 0 errors. All Task 1 and Task 2 acceptance greps pass (BuildPayload|CoerceByType=4, IsReadOnly>=1, TurboInvalidPage|TurboRedirect=6, summary heading=1, Html.Raw=0, TempData edit=0; public bool Saved=1, Role saved.=1, alert-success=1, TempData detail=0).

---
*Phase: 04-role-editing*
*Completed: 2026-06-23*
