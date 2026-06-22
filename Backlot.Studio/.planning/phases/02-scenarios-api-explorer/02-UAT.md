---
status: testing
phase: 02-scenarios-api-explorer
source: [02-VERIFICATION.md]
started: 2026-06-22T09:28:50Z
updated: 2026-06-22T09:28:50Z
---

## Current Test

number: 1
name: Scenarios page loads with grouped list
expected: |
  Navigate to /scenarios while authenticated. Page renders scenario cards grouped
  by category. No blank page, no error state.
awaiting: user response

## Tests

### 1. Scenarios page — grouped list
expected: Navigate to /scenarios → scenario cards render grouped by category. Loading state visible briefly on slow connections. "Scenarios" nav link is highlighted in sidebar.
result: [pending]

### 2. Empty state
expected: When no scenarios exist, the page shows the empty-state card ("No scenarios registered") rather than a blank page or error.
result: [pending]

### 3. Scalar side panel opens
expected: Click "Open API Docs" on any scenario card → the Scalar panel slides in from the right with the OpenAPI spec rendered.
result: [pending]

### 4. Scalar panel survives Turbo Drive navigation
expected: Open the Scalar panel, navigate to another page (e.g. Home) and back via Turbo Drive. Panel state resets (closed) but Scalar does not re-initialize from scratch — no console errors, no blank mount point.
result: [pending]

### 5. Panel close
expected: Scalar panel closes via the × button, the backdrop click, and pressing Escape.
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
