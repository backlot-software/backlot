---
status: testing
phase: 01-foundation-auth
source: [01-VERIFICATION.md]
started: 2026-06-22T08:22:56Z
updated: 2026-06-22T08:22:56Z
---

## Current Test

number: 1
name: End-to-end login flow
expected: |
  Navigate to / → redirected to /login. Enter valid Backlot API credentials.
  Submit → authenticated dashboard shows with username in sidebar.
awaiting: user response

## Tests

### 1. End-to-end login flow
expected: Navigate to / → redirected to /login. Enter valid credentials → landing on Index page with username displayed in sidebar identity block.
result: [pending]

### 2. Logout session clearing
expected: Click Logout → session and auth cookie cleared → re-visiting / redirects back to /login (not the dashboard).
result: [pending]

### 3. Mid-session 401 → full-page Turbo redirect
expected: When API returns 401 during a page load (e.g., credentials revoked server-side), the browser performs a full-page redirect to /login — not a Turbo Frame-scoped update.
result: [pending]

### 4. Invalid credentials → alert banner
expected: Entering wrong username/password shows a Bootstrap alert-danger banner "Invalid username or password" on the login page. Credentials are NOT stored in session.
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
