---
status: partial
phase: 04-role-editing
source: [04-VERIFICATION.md]
started: 2026-06-23T13:20:00Z
updated: 2026-06-24T00:00:00Z
---

## Current Test

[testing paused — 2 items outstanding]

## Tests

### 1. Turbo 303/422 round-trip
expected: Invalid POST -> HTTP 422 + form body re-rendered in place. Valid POST -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail. No 400 antiforgery failure. (ROADMAP SC #3, carried-forward risk A4.)
result: issue
reported: "The edit button is disabled."
severity: major

### 2. Live isvalid response shape + persist suppression
expected: Submitting invalid data shows the REAL isvalid error messages in the top-of-form alert-danger summary block (not the generic "Validation failed." fallback), and persist is NOT called on the invalid path. (Confirms isvalid casing/shape — RESEARCH A1, DTO PascalCase assumed but never confirmed under auth.)
result: blocked
blocked_by: other
reason: "Cannot reach the edit form — the Edit button is disabled (Test 1 issue)."

### 3. Locale-safe numeric coercion
expected: On a comma-decimal-locale host (de-DE / nl-NL / fr-FR), editing a Decimal/Double/Single field with value 1.5 persists as 1.5, not 15. (04-REVIEW Critical CR-01: CoerceByType uses culture-sensitive TryParse — latent data-integrity defect that only manifests off en-US.)
result: blocked
blocked_by: other
reason: "Cannot reach the edit form to save a value — the Edit button is disabled (Test 1 issue)."

## Summary

total: 3
passed: 0
issues: 1
pending: 0
skipped: 0
blocked: 2

## Gaps

- truth: "From a role detail page, the Edit button is clickable and navigates to the edit form"
  status: failed
  reason: "User reported: The edit button is disabled."
  severity: major
  test: 1
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
