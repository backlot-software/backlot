---
phase: "03"
plan: "02"
subsystem: role-detail-relations
status: complete
tags: [roles, detail, turbo-frames, related-roles, clipboard, permissions, badges]
dependency_graph:
  requires:
    - 03-01 (IBacklotApiClient.GetRoleDetailAsync + GetRoleRelationsAsync, RelationItem model, AuthenticatedPageModel pattern)
  provides:
    - /roles/{uid} Role Detail page
    - /roles/{uid}/relations Turbo Frame partial
    - DetailModel with permission/skills/field helpers
    - RelationsModel returning turbo-frame#related-roles
  affects:
    - Pages/Roles/Detail.cshtml
    - Pages/Roles/Detail.cshtml.cs
    - Pages/Roles/Relations.cshtml
    - Pages/Roles/Relations.cshtml.cs
tech_stack:
  added: []
  patterns:
    - JsonElement helper methods (GetPermissions, GetSkills, GetNonSystemFields, GetStringField) on DetailModel
    - Layout=null on Relations.cshtml to return bare turbo-frame element for frame swap
    - turbo-frame loading=lazy for deferred related-roles fetch
    - data-turbo-frame=_top on View links inside a nested frame to force full-page navigation
    - __Permission CanWrite drives server-side Edit button rendering (btn-primary vs disabled btn-outline-secondary)
key_files:
  created:
    - Pages/Roles/Detail.cshtml
    - Pages/Roles/Detail.cshtml.cs
    - Pages/Roles/Relations.cshtml
    - Pages/Roles/Relations.cshtml.cs
  modified: []
decisions:
  - GetPermissions uses TryGetProperty + ValueKind==True guard (not GetBoolean) to safely handle missing/null permission fields
  - RelationsModel returns Page() when Uid is empty (rather than redirect) so the frame swap still works
  - Relations.cshtml uses Layout=null — returns bare turbo-frame content so the frame swap only replaces the frame element, not a full HTML page
  - CanWrite property on DetailModel is derived once in OnGetAsync and stored; Permissions computed property re-computes from RoleData for the view
metrics:
  duration_min: 2
  completed: "2026-06-22T17:35:00Z"
  tasks_completed: 2
  tasks_total: 2
  files_created: 4
  files_modified: 0
---

# Phase 03 Plan 02: Role Detail Page and Related Roles Partial Summary

**One-liner:** Role detail page at /roles/{uid} with JsonElement field helpers, __Permission/\_\_Skills badges, CanWrite-gated Edit button, and lazy-loaded Turbo Frame relations partial at /roles/{uid}/relations.

## What Was Built

### Task 1: Role Detail PageModel and view (commit: 774d94d)

Created `Pages/Roles/Detail.cshtml.cs` (`DetailModel : AuthenticatedPageModel`) with:
- `[Authorize]` attribute
- `[BindProperty(SupportsGet = true)]` Uid routed from the page directive
- `OnGetAsync()` calling `SetUserContext()` then `SafeApiCall(_api.GetRoleDetailAsync(Uid))`
- `CanWrite` derived from `__Permission.CanWrite` after successful fetch
- Static helper methods: `GetStringField`, `GetSkills`, `GetPermissions`, `GetNonSystemFields`, `GetPageTitle`
- Computed properties: `PageTitle`, `Permissions`, `Skills`, `LastModifiedDate`, `Fields`, `CanWrite`

Created `Pages/Roles/Detail.cshtml` at route `/roles/{uid}` with:
- Back to Roles link above the heading
- Error alert (`alert-danger`) when `ErrorMessage != null` — short-circuits page body render
- Page heading: `<h4 class="fw-semibold mb-3">` with page title from primary skill
- Sub-heading: UID in `<code>`, copy button (`data-action="copy-uid"`), and Edit Role affordance
- Edit Role: `btn-primary` anchor link when `CanWrite`, disabled `btn-outline-secondary` button when not
- Fields table (`table table-sm table-bordered`, 30/70 colgroup): `__Permission` badges, `__Skills` badges, `__LastModifiedDate`, then all non-system key/value rows
- `__Permission` badges: `bg-success text-white` for true, `bg-secondary text-white` for false — CanCreate, CanRead, CanWrite
- `__Skills` badges: `bg-light text-dark border` per skill, or "None" when empty
- `<turbo-frame id="related-roles" src="/roles/@Model.Uid/relations" loading="lazy">` with placeholder text

### Task 2: Related Roles partial (commit: 8e33e2d)

Created `Pages/Roles/Relations.cshtml.cs` (`RelationsModel : AuthenticatedPageModel`) with:
- `[Authorize]` attribute
- `[BindProperty(SupportsGet = true)]` Uid from route
- `OnGetAsync()` calling `SetUserContext()` then `SafeApiCall(_api.GetRoleRelationsAsync(Uid))`
- `Relations` typed as `IEnumerable<RelationItem>`, defaults to empty

Created `Pages/Roles/Relations.cshtml` at route `/roles/{uid}/relations` with:
- `@{ Layout = null; }` — returns only the frame element, no page shell
- Entire content wrapped in `<turbo-frame id="related-roles">` for proper frame swap
- Error state: `alert alert-warning` (not alert-danger) with Retry link — per UI-SPEC
- Empty state: `<p class="text-muted small">No related roles.</p>`
- Relations table (`table table-sm table-hover`): Uid (copy button + 12-char truncated monospace + title tooltip), Info, View button
- View links: `data-turbo-frame="_top" data-turbo-action="advance"` to escape the frame and navigate full-page

## Requirements Satisfied

| Requirement | Description | Status |
|-------------|-------------|--------|
| DETL-01 | Role detail at /roles/{uid} with full field set, __Permission badges, __Skills badges | Satisfied |
| DETL-02 | Related roles load lazily in turbo-frame; View links navigate full-page via _top | Satisfied |
| DETL-03 | Copy-to-clipboard on detail heading UID and relations table UIDs (same data-action=copy-uid convention) | Satisfied |
| ROLE-03 | CanWrite=true → btn-primary Edit Role link; CanWrite=false → disabled btn-outline-secondary | Satisfied |

All 6 phase requirements across Plans 01 and 02 now satisfied:
- ROLE-01: /roles paginated list (Plan 01 Task 2)
- ROLE-02: field:value search with Turbo Frame updates (Plan 01 Task 2)
- ROLE-03: CanWrite-gated Edit button (this plan Task 1)
- DETL-01: dynamic field table with system field badges (this plan Task 1)
- DETL-02: lazy related roles frame with full-page View navigation (this plan Task 2)
- DETL-03: copy-uid convention on detail heading + relations rows (this plan Tasks 1 & 2)

## Deviations from Plan

None — plan executed exactly as written.

`GetPermissions` uses `ValueKind == JsonValueKind.True` guard rather than `GetBoolean()` to safely handle missing or unexpected value kinds without throwing, which is a more defensive implementation aligned with the plan's "default to false" intent.

## Threat Flags

None — all surfaces match the plan's threat model:
- T-03-05: uid forwarded as-is to API POST body; no server-side parsing (mitigated as planned)
- T-03-06: full field set rendered; Studio is authenticated management tool (accepted as planned)
- T-03-07: Relations page carries [Authorize]; cookie auth gates the route (mitigated as planned)
- T-03-08: disabled Edit button is UX hint only; Phase 4 enforces server-side (accepted as planned)

## Known Stubs

None — Detail page and Relations partial both consume live API data through GetRoleDetailAsync and GetRoleRelationsAsync respectively. No hardcoded values or placeholder data flows to the UI.

## Self-Check: PASSED

| Item | Status |
|------|--------|
| Pages/Roles/Detail.cshtml exists with @page "/roles/{uid}" | FOUND |
| Pages/Roles/Detail.cshtml.cs with [Authorize] + AuthenticatedPageModel | FOUND |
| Pages/Roles/Relations.cshtml exists with @page "/roles/{uid}/relations" | FOUND |
| Pages/Roles/Relations.cshtml.cs with [Authorize] + AuthenticatedPageModel | FOUND |
| Detail.cshtml has turbo-frame id=related-roles with loading=lazy | FOUND |
| Detail.cshtml has btn-primary Edit link when CanWrite, disabled btn-outline-secondary when not | FOUND |
| Detail.cshtml has __Permission badges bg-success/bg-secondary | FOUND |
| Detail.cshtml has __Skills badges bg-light text-dark border | FOUND |
| Relations.cshtml has Layout = null | FOUND |
| Relations.cshtml wraps all content in turbo-frame id=related-roles | FOUND |
| Relations.cshtml View links have data-turbo-frame=_top | FOUND |
| Relations.cshtml error uses alert-warning (not alert-danger) | FOUND |
| Relations.cshtml copy-uid buttons use data-action=copy-uid data-uid pattern | FOUND |
| dotnet build: 0 errors, 0 warnings | PASSED |
| Commit 774d94d (Task 1) | FOUND |
| Commit 8e33e2d (Task 2) | FOUND |
