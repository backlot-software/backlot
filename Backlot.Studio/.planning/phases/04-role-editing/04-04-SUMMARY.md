---
phase: 04-role-editing
plan: 04
subsystem: role-editing
status: complete
tags: [gap-closure, edit-form, matchschema, uat-blocker]
requires:
  - "04-03 (edit save path, payload build, coercion)"
provides:
  - "Edit form renders schema-driven fields (MatchSchema fixed)"
  - "Page title shows concrete role name"
  - "Disambiguated empty-state copy"
affects:
  - "Pages/Roles/Edit.cshtml.cs"
  - "Pages/Roles/Edit.cshtml"
  - "Pages/Roles/Detail.cshtml.cs"
tech-stack:
  added: []
  patterns:
    - "Most-derived-skill schema-row selection (reverse-iterate __Skills)"
key-files:
  created: []
  modified:
    - "Pages/Roles/Edit.cshtml.cs"
    - "Pages/Roles/Edit.cshtml"
    - "Pages/Roles/Detail.cshtml.cs"
decisions:
  - "MatchSchema selects the role's own/most-derived skill (last __Skills entry that has a schema row), not __Skills[0]"
  - "GetPageTitle uses LastOrDefault() to surface the concrete role name"
  - "WR-06 reconciled: false __Skills[0] premise corrected; single-deterministic-row / most-derived / never-wrong-contract intent preserved"
metrics:
  duration: "2 min"
  completed: "2026-06-24"
requirements: [EDIT-01, EDIT-02, EDIT-03]
---

# Phase 04 Plan 04: Edit Form Renders No Fields (Gap Closure) Summary

Fixed the Phase 04 UAT blocker — the role edit form rendered zero editable fields because `MatchSchema` keyed off `__Skills[0]` (a base marker like "Persist") instead of the role's own concrete name; selection now walks `__Skills` most-derived-first and binds the correct schema row.

## What Was Built

- **Task 1 — `MatchSchema` fix (EDIT-01, WR-06 reconciled):** Rewrote the selection body in `Pages/Roles/Edit.cshtml.cs` to materialize `DetailModel.GetSkills(detail)` and iterate it in REVERSE (most-derived first), returning the first schema row whose `Role` matches a skill case-insensitively, else `null`. Both the GET render (line 52) and POST save (line 97) share this one method, so a single edit fixes both paths. Also fixed `GetPageTitle` in `Pages/Roles/Detail.cshtml.cs` to use `LastOrDefault() ?? "Role"` so the title/header shows the concrete role name (e.g. "Message") instead of "Persist". The method comment documents the corrected most-derived selection and reconciles WR-06 (its `__Skills[0]` premise was false — `__Skills[0]` is a base marker — which is exactly why the form rendered no fields; its single-deterministic-row, never-wrong-contract intent is preserved).

- **Task 2 — Empty-state copy disambiguation:** In `Pages/Roles/Edit.cshtml`, Branch A (`Schema == null`) now reads "No editable schema is visible for this role…" and attributes it to account/scenario access (the visibility-filter case from `Roles.cs:82-89`). Branch B (`Schema != null && no fields`) now reads "This role's schema was found, but it defines no editable fields." Neither references internal type/reflection names. The field-rendering branch and form are unchanged.

- **Task 3 — Deterministic MatchSchema harness:** A throwaway console at the session scratchpad (`…/38306d7c-…/scratchpad/matchschema-check/`, NOT committed) copies the corrected selection body verbatim and asserts five cases. All PASS, exit 0:

```
PASS: CONCRETE-LAST — selected=Message (expected Message)
PASS: PREFER-MOST-DERIVED — selected=Message (expected Message, not Persist)
PASS: CASE-INSENSITIVE — selected=Message (expected Message)
PASS: NO-MATCH — selected=null (expected null)
PASS: EMPTY — selected=null (expected null)

ALL CASES PASSED
```

The harnessed selection body is byte-identical to the shipped `MatchSchema` (reverse-iterate the skill list, first case-insensitive `schema.Role` match, else null). CONCRETE-LAST confirms the exact UAT-blocking scenario (`__Skills = [Persist, Permission, Role, Uid, Message]`, schema row keyed "Message") now resolves to the "Message" row instead of returning null. PREFER-MOST-DERIVED confirms the most-derived skill wins over a coincidental base-marker schema row.

## WR-06 Reconciliation

WR-06 ("match the primary skill == `__Skills[0]` ONLY; do not fall back to secondary skills") rested on the FALSE premise that `__Skills[0]` is the role's own type. `Type.GetInterfaces()` (`Backlot.Core/Loader.cs:280-296`) lists inherited base markers (Persist, Permission, Role, Uid) FIRST and appends the role's own concrete name LAST, so `__Skills[0]` is a base marker and the old match never resolved the concrete role row — hence zero fields. The corrected logic still binds exactly ONE deterministic schema row, PREFERS the role's own/most-derived contract, and returns `null` (explicit no-schema state) when nothing matches. WR-06's anti-mismatch intent is preserved; only its mechanism is corrected. Documented in the `MatchSchema` method comment, citing EDIT-01.

## Verification Results

- `dotnet build Backlot.Studio/Backlot.Studio.csproj`: succeeds, 0 warnings, 0 errors (run after each task).
- MatchSchema harness: CONCRETE-LAST / PREFER-MOST-DERIVED / CASE-INSENSITIVE / NO-MATCH / EMPTY all PASS, exit 0.
- Code inspection: `MatchSchema` reverse-iterates the skill set and matches `schema.Role`; no `FirstOrDefault()`/`__Skills[0]` match key remains; `GetPageTitle` uses `LastOrDefault()`; `OnPostAsync` reuses the shared `MatchSchema`. `BuildPayload`, `CoerceByType`, `IsReadOnly`, `IsBool`, `IsNumeric`, and the field-seeding loop are unchanged.
- Empty-state copy distinguishes the visibility-filtered case from the field-less case.

## Task 4 — Live UAT (PENDING HUMAN CHECKPOINT)

Task 4 is a `checkpoint:human-verify` with `gate="blocking"`. The live re-run of UAT Tests 2-5 against the authenticated edit page was NOT performed by the executor — it is returned to the orchestrator as an AWAITING HUMAN CHECKPOINT. It remains gated on the carried-forward blocker A1/A4: the Demo.Web host runs but only a hashed password was available (no plaintext Basic Auth creds). The human must obtain plaintext credentials, run the Backlot API + Studio, and verify:

- UAT Test 2 — Form renders editable fields pre-filled, type-matched widgets, disabled Calculated, Uid+copy, Save+Cancel (the test that previously showed zero fields).
- UAT Test 3 — Invalid save → 422 re-render in place with validation summary + preserved values.
- UAT Test 4 — Valid save → 303 to `/roles/{uid}?saved=1` with the green "Role saved." banner; detail reflects saved values.
- UAT Test 5 — Decimal/Double/Single round-trips 1.5 as 1.5 (ideally on a comma-decimal locale host).
- Optional regression — empty-state copy for a visibility-filtered role.

`.planning/phases/04-role-editing/04-UAT.md` Tests 2-5 to be updated by the human based on observed results.

## Deviations from Plan

None — plan executed exactly as written. (The scratch harness used the current session scratchpad path per the orchestrator note; the plan's referenced path belonged to an expired session. The harness is not committed either way.)

## Known Stubs

None.

## Self-Check: PASSED
