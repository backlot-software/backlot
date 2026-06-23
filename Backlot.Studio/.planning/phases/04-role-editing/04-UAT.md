---
status: testing
phase: 04-role-editing
source: [04-VERIFICATION.md]
started: 2026-06-23T13:20:00Z
updated: 2026-06-23T13:20:00Z
---

## Current Test

number: 1
name: Turbo 303/422 round-trip against a running authenticated Backlot API
expected: |
  Against a running Backlot API with valid Basic Auth credentials: from a role
  detail page click Edit (Turbo Drive nav), submit invalid data, then submit
  valid data, watching the network tab.
  Invalid -> HTTP 422 + form body re-rendered in place.
  Valid -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail.
  No 400 antiforgery error. (ROADMAP SC #3, carried-forward risk A4.)
awaiting: user response

## Tests

### 1. Turbo 303/422 round-trip
expected: Invalid POST -> HTTP 422 + form body re-rendered in place. Valid POST -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail. No 400 antiforgery failure. (ROADMAP SC #3, carried-forward risk A4.)
result: [pending]

### 2. Live isvalid response shape + persist suppression
expected: Submitting invalid data shows the REAL isvalid error messages in the top-of-form alert-danger summary block (not the generic "Validation failed." fallback), and persist is NOT called on the invalid path. (Confirms isvalid casing/shape — RESEARCH A1, DTO PascalCase assumed but never confirmed under auth.)
result: [pending]

### 3. Locale-safe numeric coercion
expected: On a comma-decimal-locale host (de-DE / nl-NL / fr-FR), editing a Decimal/Double/Single field with value 1.5 persists as 1.5, not 15. (04-REVIEW Critical CR-01: CoerceByType uses culture-sensitive TryParse — latent data-integrity defect that only manifests off en-US.)
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
