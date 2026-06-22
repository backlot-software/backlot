# Phase 3: role-browsing-detail - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Two Razor Pages + one partial that deliver role browsing and detail inspection. A user can find any persisted role via paginated list with field:value search, open its detail page to inspect all fields with special rendering for `__Permission` and `__Skills`, view related roles (Uid + Info + navigate), and copy any UID to the clipboard. The "Roles" sidebar link is activated. No editing in this phase.

</domain>

<decisions>
## Implementation Decisions

### Search / Filter Behavior

- **D-01:** Search bar uses `field:value` syntax. Parser: if input contains `:`, split on first colon → `Field` (left) and `Value` (right), map to `Criteria: [{Field, Condition: 'Contains', Value}]`. If no colon, fall back to Contains match on both `Name` and `Uid` (two Criteria entries — researcher to confirm whether OR semantics are supported by the API or if sequential fallback is needed).
- **D-02:** Search input placeholder: `"Name:John or Uid:abc123 or plain text"` to communicate the syntax.
- **D-03:** Condition is always `Contains` (never Equals or StartsWith) for v1 simplicity.

### Role List Columns

- **D-04:** Default column set for mixed-type view (all roles): `Uid`, `Name`, `LastModified`. This is the displayed column set when no per-skill config exists or when multiple role types are visible.
- **D-05:** The role's type(s) are identified by `__Skills` — this array contains the interface/role names (e.g., `["Product", "Persist", "Uid"]`). Display the first entry in `__Skills` as the "Type" indicator in a Role Type column.
- **D-06:** Column configuration is per-skill-type, stored in `localStorage` under a key like `studio_columns_{skillType}`. Accessible via a gear icon near the column headers on the list page. Gear opens an inline panel showing available fields from the current result set with checkboxes; changes save immediately to localStorage.
- **D-07:** Per-skill config only applies when all visible roles share the same primary skill type (i.e., the list is filtered to a single type via search). Mixed-type view always uses the default columns (D-04).

### Related Roles Display

- **D-08:** The related roles Turbo Frame shows three columns: `Uid` (truncated, with copy-to-clipboard button), `Info` (the relation description string from the API), and a "View" button linking to `/roles/{uid}`. No Role Type column — the `persist/relations` response does not include type information and per-relation `seekbase/detail` calls would violate the no-N+1 rule.

### System Field Visibility on Detail Page

- **D-09:** Fields rendered in the detail table (in order at the top): `__Permission` (badge rendering — CanCreate, CanRead, CanWrite), `__Skills` (badge rendering per skill), `__LastModifiedDate` (rendered as raw ISO string). All other `__` prefixed fields are hidden. All non-`__` fields are rendered below as plain key/value rows.
- **D-10:** All `__` system fields shown on detail page are read-only (no edit affordance, no special interaction beyond the permission-gated Edit button).

### Claude's Discretion

- C# model types for dynamic role data (e.g., `Dictionary<string, JsonElement>` vs `JsonElement` for `seekbase/detail` body and `simplequery/find` results)
- Razor Page route structure for `/roles/{uid}/relations` partial (separate page vs named handler)
- Exact localStorage key schema for column config
- Turbo Frame `src` construction for search/pagination URL params
- `For` parameter value for "all roles" query in `simplequery/find` (null or empty string)
- UID truncation length (12 chars per UI-SPEC) and `title` tooltip implementation

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Constraints & Architecture
- `.planning/PROJECT.md` — Core value, constraints (Razor Pages + Turbo + Bootstrap, no SPA), auth model
- `.planning/REQUIREMENTS.md` — ROLE-01 through ROLE-03, DETL-01 through DETL-03 (the 6 requirements this phase must satisfy)
- `.planning/ROADMAP.md` §Phase 3 — Goal, success criteria, plan breakdown (03-01 role list, 03-02 role detail)

### Tech Stack (from CLAUDE.md)
- `.claude/CLAUDE.md` — Pinned versions (Bootstrap 5.3.8, Turbo 8.0.23, .NET 10), CDN patterns, auth handler pattern, session config, "What NOT to Use" list

### UI Design Contract
- `.planning/phases/03-role-browsing-detail/03-UI-SPEC.md` — Complete visual and interaction contract: layout, component inventory, states, copywriting, spacing, color, Turbo specifics. Downstream agents MUST read this before implementing any frontend for this phase.

### API Endpoints (from openapidoc.json)
- `wwwroot/openapidoc.json` — OpenAPI spec; relevant endpoints for this phase:
  - `POST /api/role/simplequery/find` — body: `{For?, Criteria[{Field, Condition, Value}]?, PageSize, Page}`, response: `{Body: {Page, PageSize, Total, Results: object[]}}`
  - `POST /api/role/seekbase/detail` — body: `{For: uid}`, response: `{Body: object}` (dynamic)
  - `POST /api/role/persist/relations` — body: `{Uid: uid}`, response: `{Body: [{Uid, Info}]}`

### Existing Patterns to Follow
- `Pages/Scenarios/Index.cshtml` + `Pages/Scenarios/Index.cshtml.cs` — closest analog: paginated/listed page using `AuthenticatedPageModel`, `SafeApiCall`, and error/empty state pattern
- `Services/BacklotApiClient.cs` — existing typed client (GET only; this phase adds POST methods)
- `Services/IBacklotApiClient.cs` — interface to extend with new methods
- `Pages/Shared/_Sidebar.cshtml` — upgrade "Roles" from disabled span to active `<a href="/roles">` link

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AuthenticatedPageModel` (`Pages/AuthenticatedPageModel.cs`) — provides `SetUserContext()` (populates `ViewData["Username"]`) and `SafeApiCall<T>()` (catches `BacklotApiUnauthorizedException` and redirects to `/Login`). Both new page models must inherit from this.
- `ApiEnvelope<T>` (`Services/ApiEnvelope.cs`) — wraps `{Body, Status, TimeInMs, ExecutionTime}`. Use `ApiEnvelope<FindResult>` for list, `ApiEnvelope<Dictionary<string, JsonElement>>` for detail (or similar dynamic type).
- `BacklotApiClient` / `IBacklotApiClient` — currently has `GetEnvelopeAsync<T>` (GET). This phase adds `PostEnvelopeAsync<T>` (POST with JSON body) as a new private helper and public methods: `FindRolesAsync`, `GetRoleDetailAsync`, `GetRoleRelationsAsync`.

### Established Patterns
- Page error handling: `catch (HttpRequestException or TaskCanceledException)` → set `ErrorMessage` property → render `alert alert-danger` in the view. Same pattern as Scenarios page.
- Layout activation: `ViewData["ActiveNav"] = "roles"` in the page handler → `_Sidebar.cshtml` applies `.active fw-semibold text-primary` class.
- 401 handling: `SafeApiCall<T>` wraps API calls and returns a `RedirectToPage("/Login")` result on `BacklotApiUnauthorizedException`. Non-frame pages set `Response.Headers["Turbo-Visit-Control"] = "reload"` to force a full navigation (avoids 401 appearing inside a Turbo Frame).

### Integration Points
- `_Sidebar.cshtml`: the `disabled` span for "Roles" becomes a functional `<a href="/roles" class="nav-link @(activeNav == "roles" ? "active fw-semibold text-primary" : "text-muted")">` link.
- `wwwroot/js/studio.js`: extend the `turbo:load` listener to add event delegation for `[data-action="copy-uid"]` buttons (clipboard write + icon swap + revert after 1500ms).
- Razor Pages routing: `/roles/{uid}/relations` needs a separate page (likely `Pages/Roles/Relations.cshtml` with `@page "/roles/{uid}/relations"`) to serve the Turbo Frame partial.

</code_context>

<specifics>
## Specific Ideas

- The `field:value` search syntax should be documented in the placeholder text itself: `"Name:John or Uid:abc123 or plain text"`.
- `__Skills` array first entry = primary role type for the Type column in the list table.
- LocalStorage column config key pattern: `studio_columns_{primarySkill}` (e.g., `studio_columns_Product`).
- Related roles table: Uid (truncated 12 chars, copy button) + Info string + "View" button. No type column.
- `__LastModifiedDate` renders as raw ISO string in the Fields table (no formatting).
- Detail page system field order at top of table: `__Permission` → `__Skills` → `__LastModifiedDate` → then all non-`__` fields alphabetically or in API-returned order.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 3-role-browsing-detail*
*Context gathered: 2026-06-22*
