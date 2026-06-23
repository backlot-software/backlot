# Roadmap: Backlot.Studio

## Overview

Backlot.Studio is built as a thin, server-rendered presentation layer over the Backlot REST API. The journey starts by standing up the project and the foundation that gates everything: a typed HttpClient service layer that unwraps the API envelope and injects Basic Auth credentials read from server-side session, plus the login/logout/401 auth boundary. With that foundation in place we layer on the read surfaces — first the scenario overview and embedded Scalar API explorer, then the core role discovery and inspection surface (browse, search, detail, relations). The final phase isolates the only mutation flow in v1, the schema-driven role edit form, where the Turbo 303/422 and antiforgery hazards are concentrated. Each phase builds complete capability on top of the layer beneath it.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation & Auth** - Project scaffold, typed API service layer, and session-based Basic Auth boundary that gates everything (completed 2026-06-22)
- [x] **Phase 2: Scenarios & API Explorer** - Scenario overview page plus the slide-in Scalar API reference panel (completed 2026-06-22)
- [x] **Phase 3: Role Browsing & Detail** - Searchable paginated role list, role detail with permissions/skills, and clickable related-roles navigation (completed 2026-06-22)
- [ ] **Phase 4: Role Editing** - Schema-driven edit form with inline validation and Turbo-safe save (plans complete; awaiting human verification)

## Phase Details

### Phase 1: Foundation & Auth

**Goal**: A user can log in to Studio and reach an authenticated shell, with all API access flowing through a typed service layer that injects session-held Basic Auth credentials and handles auth failures cleanly.
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04
**Success Criteria** (what must be TRUE):

  1. User can log in with username and password; credentials are base64-encoded and stored server-side in session, never exposed to the browser
  2. User can log out, which clears the server session and returns them to the login page
  3. When the API returns 401 (expired/invalid credentials), the user is redirected to the login page at the top level (not inside a Turbo Frame)
  4. An authenticated user sees their current identity (from `whoami`) in the navbar of the Bootstrap + Turbo shell
  5. Every outbound API call is issued by a pooled typed HttpClient with the Basic Auth header injected by a `DelegatingHandler` reading session per request (no `new HttpClient()`)

**Plans**: 3/3 plans complete

Plans:
**Wave 1**

- [x] 01-01-PLAN.md — Project scaffold: `Backlot.Studio.csproj` added to solution, Bootstrap 5.3.8 + Turbo 8.0.23 CDN shell (`_Layout.cshtml`, `_LoginLayout.cshtml`, `_Sidebar.cshtml`), sidebar collapse CSS/JS, `appsettings.json`

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 01-02-PLAN.md — Typed API service layer: `IBacklotApiClient`/`BacklotApiClient`, `ApiEnvelope<T>`, `BasicAuthHandler` (IHttpContextAccessor, session-read-in-SendAsync), `BacklotApiUnauthorizedException`, `AuthenticatedPageModel` (Turbo-Visit-Control 401 redirect), Program.cs DI wiring

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 01-03-PLAN.md — Auth flow pages: Login (centered card, credential validation, session store, SignIn), Logout (Session.Clear + SignOut), Index (whoami → ViewData["Username"] in sidebar)

### Phase 2: Scenarios & API Explorer

**Goal**: A user can browse all registered scenarios and open an interactive Scalar API reference for any of them, proving the end-to-end auth + fetch + render path on read-only pages and isolating the riskiest third-party-JS + Turbo integration.
**Depends on**: Phase 1
**Requirements**: SCEN-01, SCEN-02
**Success Criteria** (what must be TRUE):

  1. User can view a list of all registered scenarios (from `director/scenarios`), grouped or tagged by category, with loading/empty/error states
  2. User can open a slide-in Scalar API reference side panel keyed to any scenario and see its interactive endpoint docs
  3. The Scalar panel re-initializes correctly after Turbo navigations (no blanking, duplication, or stale content) and uses a pinned CDN version

**Plans**: 2/2 plans complete
**UI hint**: yes

Plans:

- [x] 02-01-PLAN.md — Scenario overview page: `/scenarios` DTOs + `GetScenariosAsync`, grouped cards, empty/error states, active sidebar nav (SCEN-01)
- [x] 02-02-PLAN.md — Scalar side panel: `openapidoc.json` served from wwwroot, `data-turbo-permanent` mount, single-init on `turbo:load`, open/close/Escape, pinned 1.60.0 (SCEN-02)

### Phase 3: Role Browsing & Detail

**Goal**: A user can discover any role via search and pagination, open its detail page to inspect fields/permissions/skills, and navigate to related roles — delivering two-thirds of the Core Value (find and inspect).
**Depends on**: Phase 1
**Requirements**: ROLE-01, ROLE-02, ROLE-03, DETL-01, DETL-02, DETL-03
**Success Criteria** (what must be TRUE):

  1. User can browse a paginated list of all roles (`simplequery/find`, page size 25) showing the total count, rendered from the find response alone (no per-row N+1 fetches)
  2. User can search/filter roles by field via `Criteria`, see a result count, and clear the search; updates happen in-place via a Turbo Frame with server-side paging
  3. User can open a role detail page (`seekbase/detail`) showing the full field set with `__Permission` and `__Skills` badges
  4. User can see related roles (`persist/relations`) and click any of them to navigate to its detail page
  5. The Edit action is hidden or disabled wherever `__Permission.CanWrite` is false, and any UID can be copied to the clipboard with one click

**Plans**: 2/2 plans complete

Plans:
**Wave 1**

- [x] 03-01-PLAN.md — Service layer (PostEnvelopeAsync + FindRolesAsync/GetRoleDetailAsync/GetRoleRelationsAsync), Role List page /roles with Turbo Frame search + pagination, sidebar Roles link, copy-uid JS

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 03-02-PLAN.md — Role Detail page /roles/{uid} with dynamic fields + __Permission/__Skills badges + CanWrite-gated Edit, Related Roles turbo-frame partial /roles/{uid}/relations

### Phase 4: Role Editing

**Goal**: A user can edit any writable role through a schema-driven form, see inline validation feedback, and save changes — completing the final Core Value pillar (mutate) while isolating the Turbo form hazards in one place.
**Depends on**: Phase 3
**Requirements**: EDIT-01, EDIT-02, EDIT-03
**Success Criteria** (what must be TRUE):

  1. User can navigate to `/roles/{uid}/edit` and see a form with all editable fields, rendered from the role's field schema (`director/roles`)
  2. Before saving, field-level validation errors from `role/isvalid` are shown inline next to the relevant fields
  3. User can save via `persist/persist`; on success they are redirected (303) to the role detail page, and on validation failure the form re-renders (422) with errors visible — including after a prior Turbo navigation

**Plans**: 2/2 plans complete
**UI hint**: yes

Plans:
**Wave 1**

- [x] 04-01-PLAN.md — Service layer (GetRoleSchemaAsync/ValidateRoleAsync/PersistRoleAsync) + RoleSchema/ValidationOutcome DTOs + TurboEditPageModel (303/422 helpers) + schema-driven `/roles/{uid}/edit` form; front-loaded Turbo 303/422 smoke test against the running API (EDIT-01)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 04-02-PLAN.md — Production save orchestration: mass-assignment-safe BuildPayload, `role/isvalid` summary-block errors (422, D-07), `persist/persist` save (303), and the TempData-free `?saved=1` "Role saved." banner on the detail page (D-08) (EDIT-02, EDIT-03)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Auth | 3/3 | Complete   | 2026-06-22 |
| 2. Scenarios & API Explorer | 2/2 | Complete   | 2026-06-22 |
| 3. Role Browsing & Detail | 2/2 | Complete   | 2026-06-22 |
| 4. Role Editing | 2/2 | Verifying  | —          |
