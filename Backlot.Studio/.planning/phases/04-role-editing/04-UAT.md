---
status: partial
phase: 04-role-editing
source: [04-01-SUMMARY.md, 04-02-SUMMARY.md, 04-03-SUMMARY.md]
started: 2026-06-24T12:00:00Z
updated: 2026-06-24T12:10:00Z
---

## Current Test

[testing paused — 3 items outstanding]

## Tests

### 1. Edit button enabled + navigation
expected: On a writable role's detail page, the "Edit" control is an enabled link (not disabled/greyed). Clicking it navigates to /roles/{uid}/edit and the edit form loads.
result: pass

### 2. Schema-driven edit form renders
expected: The edit form lists all editable fields pre-filled with the role's current values. Widgets match field type — checkbox for Boolean, number input for numeric fields, text input otherwise. Calculated/read-only fields appear disabled. The Uid is shown (with copy). A single blue Save button and an outline Cancel are present.
result: issue
reported: "I don't see any fields to edit. Based on the role fields of the skills edit fields have to be created."
severity: major

### 3. Invalid save → 422 + validation summary in place
expected: Submitting the form with invalid data re-renders the form in place (no full-page reload) with a top-of-form red validation summary ("Please fix the following before saving:") listing the real validation messages from role/isvalid. The values you entered are preserved and nothing is persisted.
result: blocked
blocked_by: other
reason: "Can not submit the form since there is no form rendered (gated on Test 2 — no edit fields rendered)."

### 4. Valid save → 303 + "Role saved." banner
expected: Submitting valid data navigates to the role's detail page (/roles/{uid}?saved=1) showing a dismissible green "Role saved." success banner above the title. The detail page reflects the saved values.
result: blocked
blocked_by: other
reason: "Same as Test 3 — no form rendered (gated on Test 2)."

### 5. Locale-safe numeric coercion
expected: On a comma-decimal-locale host (de-DE / nl-NL / fr-FR), editing a Decimal/Double/Single field to 1.5 persists as 1.5 (not 15). Numeric values round-trip correctly regardless of host culture (CoerceByType uses InvariantCulture).
result: blocked
blocked_by: other
reason: "Same as Tests 3 and 4 — no form rendered (gated on Test 2)."

### 6. Permission enforcement on read-only role
expected: For a read-only role (CanWrite=false), the detail page does not offer an enabled Edit link, and a forced edit POST is rejected with a 422 re-render carrying "You don't have permission to edit this role." — never silently persisted.
result: pass

## Summary

total: 6
passed: 2
issues: 1
pending: 0
skipped: 0
blocked: 3

## Gaps

- truth: "The edit form lists all editable fields pre-filled with the role's current values, with type-matched widgets"
  status: failed
  reason: "User reported: I don't see any fields to edit. Based on the role fields of the skills edit fields have to be created."
  severity: major
  test: 2
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
