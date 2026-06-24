---
status: testing
phase: 04-role-editing
source: [04-VERIFICATION.md]
started: 2026-06-23T13:20:00Z
updated: 2026-06-24T00:00:00Z
---

## Current Test

number: 1
name: Turbo 303/422 round-trip (Edit button now enabled)
expected: |
  From a role detail page for a writable role, the Edit button is an enabled link and
  navigates to /roles/{uid}/edit. Invalid POST -> HTTP 422 + form body re-rendered in
  place. Valid POST -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to
  detail with the "Role saved." banner. No 400 antiforgery failure.
awaiting: user response

## Tests

### 1. Turbo 303/422 round-trip
expected: From a role detail page for a writable role, the Edit button is an enabled link and navigates to /roles/{uid}/edit. Invalid POST -> HTTP 422 + form body re-rendered in place. Valid POST -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail. No 400 antiforgery failure. (ROADMAP SC #3, carried-forward risk A4.)
result: pending
note: "Edit-button blocker fixed by gap-closure 04-03 (defensive Role-wrapper unwrap in GetRoleDetailAsync). Re-testable once authenticated Backlot API creds are available (carried-forward blocker A1/A4)."

### 2. Live isvalid response shape + persist suppression
expected: Submitting invalid data shows the REAL isvalid error messages in the top-of-form alert-danger summary block (not the generic "Validation failed." fallback), and persist is NOT called on the invalid path. (Confirms isvalid casing/shape — RESEARCH A1, DTO PascalCase assumed but never confirmed under auth.)
result: pending
note: "Unblocked by 04-03 (Edit form now reachable). Requires authenticated API creds."

### 3. Locale-safe numeric coercion
expected: On a comma-decimal-locale host (de-DE / nl-NL / fr-FR), editing a Decimal/Double/Single field with value 1.5 persists as 1.5, not 15. (04-REVIEW Critical CR-01: CoerceByType uses culture-sensitive TryParse — latent data-integrity defect that only manifests off en-US.)
result: pending
note: "Code-level fix landed (CoerceByType now uses CultureInfo.InvariantCulture, commit e3c537b). Unblocked by 04-03. Requires authenticated API creds for live save."

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps

- truth: "From a role detail page, the Edit button is clickable and navigates to the edit form"
  status: resolved
  reason: "Fixed by gap-closure plan 04-03: defensive `UnwrapRoleDetail` helper in GetRoleDetailAsync unwraps the seekbase/detail `Role` wrapper at the single chokepoint, so __Permission/__Skills/fields read at the correct level and CanWrite enables the Edit button. Unwrap harness (WRAPPED/FLAT/NON-OBJECT) all PASS; build green."
  severity: major
  test: 1
  resolved_by: 04-03-PLAN.md
  debug_session: ".planning/debug/edit-button-disabled-detail.md"
