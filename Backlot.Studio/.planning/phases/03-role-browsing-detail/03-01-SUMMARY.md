---
phase: "03"
plan: "01"
subsystem: role-browsing-list
status: complete
tags: [roles, browsing, turbo-frames, api-client, column-config, clipboard]
dependency_graph:
  requires: []
  provides:
    - FindRolesAsync via PostEnvelopeAsync helper
    - GetRoleDetailAsync via PostEnvelopeAsync helper
    - GetRoleRelationsAsync via PostEnvelopeAsync helper
    - /roles Razor Page with turbo-frame#role-list
    - studio.js copy-uid event delegation
    - column-config.js initColumnConfig (D-06, D-07)
  affects:
    - Services/IBacklotApiClient.cs
    - Services/BacklotApiClient.cs
    - Pages/Shared/_Sidebar.cshtml
    - wwwroot/js/studio.js
tech_stack:
  added: []
  patterns:
    - PostEnvelopeAsync<T> helper (PostAsJsonAsync + EnsureSuccessStatusCode + ReadFromJsonAsync)
    - JsonElement[] for dynamic role result rows from simplequery/find
    - field:value search query parsing on server side (first colon split)
    - turbo-frame with data-turbo-action="advance" for SPA-like pagination
    - localStorage per-skill column config (studio_columns_{skillType})
    - event delegation for copy-uid (document-level, persists across frame updates)
key_files:
  created:
    - Models/Api/RoleFind.cs
    - Pages/Roles/Index.cshtml
    - Pages/Roles/Index.cshtml.cs
    - wwwroot/js/column-config.js
  modified:
    - Services/IBacklotApiClient.cs
    - Services/BacklotApiClient.cs
    - Pages/Shared/_Sidebar.cshtml
    - wwwroot/js/studio.js
decisions:
  - PostEnvelopeAsync<T> private helper follows the same pattern as GetEnvelopeAsync<T> — PostAsJsonAsync + EnsureSuccessStatusCode + ReadFromJsonAsync<ApiEnvelope<T>>
  - FindResult.Results typed as JsonElement[] — dynamic role schema cannot be pre-typed; fields extracted in PageModel helpers (GetField, GetPrimarySkill)
  - Search parsing: field:value via first-colon split → single Criteria entry; plain text → two Criteria entries (Name + Uid Contains); empty → null Criteria/For
  - copy-uid wired as top-level document click listener (not inside turbo:load) so it survives Turbo Frame partial updates that replace the table without a full page navigation
  - column-config.js is a separate file from studio.js (loaded only on Roles pages via @section Scripts)
  - Gear panel close-on-outside uses a document click listener without stopPropagation on panel body; gear button click uses stopPropagation to prevent immediate re-close
metrics:
  duration_min: 3
  completed: "2026-06-22T17:29:54Z"
  tasks_completed: 3
  tasks_total: 3
  files_created: 4
  files_modified: 4
---

# Phase 03 Plan 01: Role List Page and Service Layer Summary

**One-liner:** Paginated role browsing at /roles using PostEnvelopeAsync + FindRolesAsync with Turbo Frame search, server-side field:value parsing, clipboard copy, and per-skill localStorage column config (D-06, D-07).

## What Was Built

### Task 1: DTOs and Service Layer (commit: 83d003b)

Created `Models/Api/RoleFind.cs` containing `FindCriteria`, `FindRequest`, `FindResult` (with `JsonElement[]` Results for dynamic schema), and `RelationItem` in namespace `Backlot.Studio.Models.Api`.

Extended `IBacklotApiClient` with three new method signatures: `FindRolesAsync`, `GetRoleDetailAsync`, `GetRoleRelationsAsync`.

Added `PostEnvelopeAsync<T>` private helper to `BacklotApiClient` following the same pattern as `GetEnvelopeAsync<T>`. Implemented all three new API methods.

### Task 2: Role List Page + Sidebar + Clipboard JS (commit: f05c3c7)

Created `Pages/Roles/Index.cshtml.cs` (`IndexModel : AuthenticatedPageModel`) with:
- `[Authorize]` attribute
- `[FromQuery]` bound `SearchQuery` and `CurrentPage`
- `OnGetAsync()` calling `SetUserContext()` then `SafeApiCall(_api.FindRolesAsync(...))`
- Server-side search parsing: `field:value` → single Criteria, plain text → Name+Uid Contains, empty → null
- Read-only helpers: `TotalCount`, `StartItem`, `EndItem`, `TotalPages`
- Static helpers: `GetField(JsonElement, string)` and `GetPrimarySkill(JsonElement)` for dynamic role rows

Created `Pages/Roles/Index.cshtml` with:
- Error alert outside the Turbo Frame (always visible)
- `<turbo-frame id="role-list" data-turbo-action="advance">` wrapping search form, result count, table, and pagination
- Table with columns: UID (copy button + 12-char truncated monospace + title), Type (primary skill), Actions (View link)
- Each `<tr>` carries `data-primary-skill` and `data-fields` for column-config.js
- Each `<th>` carries `data-col` for column visibility toggling
- Bootstrap pagination rendered only when TotalPages > 1; links include `?q=` and `?page=N`
- Two empty-state variants (searching vs not)
- `@section Scripts { <script src="/js/column-config.js"></script> }` at the bottom

Upgraded `Pages/Shared/_Sidebar.cshtml`: replaced the disabled `<span>` for Roles with `<a href="/roles" class="nav-link @(activeNav == "roles" ? "active fw-semibold text-primary" : "text-muted")">`.

Extended `wwwroot/js/studio.js` with a top-level `document.addEventListener('click', ...)` handler for `[data-action="copy-uid"]` buttons: reads `data-uid`, calls `navigator.clipboard.writeText()`, swaps icon to `bi-clipboard-check`, appends visually-hidden "Copied!" span, reverts after 1500ms. Silently ignores clipboard failures.

### Task 3: Column Config Panel D-06/D-07 (commit: 77ee1ad)

Created `wwwroot/js/column-config.js` with `initColumnConfig(tableEl)`:
- Collects `data-primary-skill` values from all `<tbody tr>` to determine single-type vs mixed mode
- Mixed mode: always uses default columns `['Uid', 'Type', 'Actions']`, gear button disabled with descriptive title
- Single-type mode: reads `localStorage.getItem('studio_columns_{skillType}')`, falls back to defaults without writing
- Gear button injected into `<thead tr>` as a new `<th class="col-gear">`
- Panel injected as `<div id="col-config-panel" class="card shadow-sm ...">` inside the `.position-relative` container
- Toggleable fields: derived from `data-fields` JSON arrays on `<tr>` elements, excluding Uid and Actions (always visible)
- Checkbox `change` event: updates in-memory `activeColumns`, calls `applyColumnVisibility`, writes `localStorage.setItem('studio_columns_{skillType}', ...)` immediately
- Panel closes on outside click via `document.addEventListener('click', ...)` listener
- Wired to `turbo:load`, `turbo:frame-load` (for the `role-list` frame), and `DOMContentLoaded`

## Deviations from Plan

None — plan executed exactly as written.

The plan already included `data-primary-skill`, `data-fields` attributes on `<tr>` and `data-col` on `<th>` elements as part of Task 2 (to be used by Task 3), so no additional modifications to `Index.cshtml` were needed in Task 3 beyond what was created in Task 2.

## Threat Flags

None — all surfaces match the plan's threat model. `FindRequest.Criteria` is constructed server-side via string split only (T-03-01 mitigated). UID values in HTML are non-secret identifiers (T-03-02 accepted). Clipboard access requires a user gesture (T-03-03 accepted). localStorage values are used only for column display preferences with no server-side trust (T-03-04 accepted).

## Known Stubs

None — all data flows from the Backlot API through `FindRolesAsync`. The table renders real API results or appropriate empty/error states.

## Self-Check: PASSED

| Item | Status |
|------|--------|
| Models/Api/RoleFind.cs exists | FOUND |
| Pages/Roles/Index.cshtml exists with turbo-frame#role-list | FOUND |
| Pages/Roles/Index.cshtml.cs with [Authorize] + AuthenticatedPageModel | FOUND |
| Services/IBacklotApiClient.cs has three new method signatures | FOUND |
| Services/BacklotApiClient.cs has PostEnvelopeAsync<T> | FOUND |
| Pages/Shared/_Sidebar.cshtml has active /roles link (no disabled span) | FOUND |
| wwwroot/js/studio.js has copy-uid handler with clipboard.writeText | FOUND |
| wwwroot/js/column-config.js has initColumnConfig(tableEl) | FOUND |
| data-primary-skill on tbody tr | FOUND |
| data-fields on tbody tr | FOUND |
| data-col on thead th | FOUND |
| column-config.js script tag in Index.cshtml | FOUND |
| dotnet build: 0 errors, 0 warnings | PASSED |
| Commit 83d003b (Task 1) | FOUND |
| Commit f05c3c7 (Task 2) | FOUND |
| Commit 77ee1ad (Task 3) | FOUND |
