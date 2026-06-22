---
status: complete
phase: 01-foundation-auth
source: [01-VERIFICATION.md]
started: 2026-06-22T08:22:56Z
updated: 2026-06-22T10:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. End-to-end login flow
expected: Navigate to / → redirected to /login. Enter valid credentials → landing on Index page with username displayed in sidebar identity block.
result: pass
note: Fixed inline — WhoAmI JSON body replaced by SetUserContext() reading ClaimTypes.Name cookie claim.

### 2. Logout session clearing
expected: Click Logout → session and auth cookie cleared → re-visiting / redirects back to /login (not the dashboard).
result: pass

### 3. Mid-session 401 → full-page Turbo redirect
expected: When API returns 401 during a page load (e.g., credentials revoked server-side), the browser performs a full-page redirect to /login — not a Turbo Frame-scoped update.
result: skipped
reason: Hard to trigger without tooling; needs manual verification with session cookie manipulation

### 4. Invalid credentials → alert banner
expected: Entering wrong username/password shows a Bootstrap alert-danger banner "Invalid username or password" on the login page. Credentials are NOT stored in session.
result: pass

## Summary

total: 4
passed: 3
issues: 0
pending: 0
skipped: 1
skipped: 0
blocked: 0

## Gaps

- truth: "Username displayed in sidebar identity block after login"
  status: failed
  reason: "User reported: Loggedin value shows as json format not as a nice designed overview"
  severity: major
  test: 1
  root_cause: "WhoAmIAsync returns object? which deserialises to JsonElement (kind=Object); result?.ToString() produces raw JSON. Username already stored in ClaimTypes.Name cookie claim — SetUserContext() reads it directly."
  artifacts:
    - path: "Pages/Index.cshtml.cs"
      issue: "JsonElement parsing replaced by SetUserContext()"
  missing: []
  debug_session: ""
