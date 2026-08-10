---
status: testing
phase: 03-role-browsing-detail
source: 03-01-SUMMARY.md, 03-02-SUMMARY.md
started: 2026-06-22T18:30:00Z
updated: 2026-06-22T18:30:00Z
---

## Current Test

number: 1
name: Role List Navigation
expected: |
  Open the app, log in, and click "Roles" in the left sidebar.
  The sidebar link should be active (not greyed-out / disabled).
  The /roles page loads showing a paginated table of roles with a total count ("Showing 1–25 of N roles").
  The table has columns: UID, Name, Last Modified, Type, and an Actions column.
awaiting: user response

## Tests

### 1. Role List Navigation
expected: Open the app, log in, and click "Roles" in the left sidebar. The sidebar link should be active (not greyed-out / disabled). The /roles page loads showing a paginated table of roles with a total count ("Showing 1–25 of N roles"). The table has columns: UID, Name, Last Modified, Type, and an Actions column.
result: blocked
blocked_by: server
reason: "it returns Could not load roles. Check that the Backlot API is reachable and that your credentials are valid. Retry"

### 2. Role Search — field:value syntax
expected: In the search bar on /roles, type "Name:something" (or any field:value pair) and submit. The table updates in-place (no full page reload) showing only matching roles, with a result count ("Showing X–Y of Z roles matching "Name:something""). A "Clear" button appears alongside the search input.
result: [pending]

### 3. Role Search — plain text fallback
expected: Clear the search, then type a plain word (no colon) and submit. The table updates showing roles where Name or Uid contains that word. The result count and Clear button work the same way.
result: [pending]

### 4. Copy UID in Role List
expected: On the /roles list, click the clipboard icon next to any UID. The icon briefly changes (to a checkmark/filled clipboard) for about 1.5 seconds, then reverts. The UID value should now be in your clipboard (paste it somewhere to verify).
result: [pending]

### 5. Column Config Gear Panel
expected: On /roles, a small gear icon appears in the table header area. Clicking it opens an inline panel showing checkboxes for available columns (Name, Last Modified, Type visible; Uid and Actions always shown). Unchecking a column hides it from the table immediately. After a page refresh, the column preference is remembered (persists via localStorage).
result: [pending]

### 6. Role Detail Page
expected: Click "View" on any role in the list. The browser navigates to /roles/{uid}. The detail page shows a heading with the role's primary skill type, the full UID in a code element, and a table listing all fields. System fields appear at the top: __Permission row with CanCreate/CanRead/CanWrite badges, __Skills row with skill badges, __LastModifiedDate row with an ISO date string. All non-system fields follow below.
result: [pending]

### 7. Permission Badges
expected: On a role detail page, the __Permission row shows three badges: CanCreate, CanRead, CanWrite. Badges for permissions that are TRUE show in green (bg-success). Badges for permissions that are FALSE show in grey (bg-secondary). The labels are correct and readable.
result: [pending]

### 8. Edit Button Gating
expected: On a role detail page where CanWrite is TRUE, an "Edit Role" button (blue/primary) is visible near the heading. On a role where CanWrite is FALSE (or if you can only view), the Edit Role button is disabled (greyed out, not clickable). In both cases the button/affordance is present — it's never completely hidden.
result: [pending]

### 9. Related Roles — lazy load
expected: On a role detail page, scroll down to the "Related Roles" section. It initially shows a loading placeholder. After a moment, the section fills in with a table of related roles (or "No related roles." if none). The table shows UID (truncated, with copy button), Info text, and a View button per row.
result: [pending]

### 10. Related Role Navigation
expected: In the Related Roles table (on a detail page that has at least one related role), click the View button on any row. The browser navigates fully to that role's detail page (full-page navigation, not a frame-only update). The URL should update to /roles/{related-uid}.
result: [pending]

## Summary

total: 10
passed: 0
issues: 0
pending: 10
skipped: 0
blocked: 0

## Gaps

<!-- YAML format for plan-phase --gaps consumption -->
