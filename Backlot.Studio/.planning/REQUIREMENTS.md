# Requirements: Backlot.Studio

**Defined:** 2026-06-21
**Core Value:** A developer or operator can find any role in the system, inspect its state and relations, and edit it — without writing a single API call by hand.

## v1 Requirements

### Authentication

- [ ] **AUTH-01**: User can log in with username and password; credentials are base64-encoded as `username:password` and stored in server-side session as a Basic Auth header
- [ ] **AUTH-02**: User can log out; server session is cleared and user is redirected to the login page
- [ ] **AUTH-03**: User is automatically redirected to the login page when the Backlot API returns a 401 (expired or invalid credentials)
- [ ] **AUTH-04**: User sees their current username/identity in the navbar via `GET /api/role/director/whoami`

### Scenarios

- [ ] **SCEN-01**: User can view a list of all registered scenarios from `GET /api/role/director/scenarios`, grouped or tagged by category
- [ ] **SCEN-02**: User can open a Scalar API reference side panel (slide-in overlay) keyed to any scenario, showing the endpoint's interactive docs

### Roles

- [ ] **ROLE-01**: User can browse a paginated list of all roles via `POST /api/role/simplequery/find` (page size 25, showing total count)
- [ ] **ROLE-02**: User can search and filter roles by field using the `Criteria` parameter, with a visible result count and a "clear search" affordance
- [ ] **ROLE-03**: The Edit action on role list and detail pages is hidden or disabled when `__Permission.CanWrite` is false

### Role Detail

- [ ] **DETL-01**: User can view a role's full field set, including `__Permission` (CanCreate/CanRead/CanWrite) and `__Skills` badges, on a detail page via `POST /api/role/seekbase/detail`
- [ ] **DETL-02**: User can view a list of roles related to the current role (from `POST /api/role/persist/relations`) and click any related role to navigate to its detail page
- [ ] **DETL-03**: User can copy any UID value to the clipboard with a single click (copy-to-clipboard button alongside UID fields)

### Role Edit

- [ ] **EDIT-01**: User can navigate to `/roles/:uid/edit` and see a form with all editable fields, dynamically rendered from the role's field schema (`GET /api/role/director/roles`)
- [ ] **EDIT-02**: Before saving, field-level validation errors from `POST /api/role/role/isvalid` are shown inline next to the relevant fields
- [ ] **EDIT-03**: User can save changes to a role via `POST /api/role/persist/persist`; on success the user is redirected to the role detail page; on failure, validation errors are re-displayed

## v2 Requirements

### History

- **HIST-01**: User can view a revision history for a role via `POST /api/role/seekbase/revisions`
- **HIST-02**: User can compare two revisions of a role side by side

### Advanced Role Management

- **ADV-01**: Edit form renders schema-aware widget hints from `Fields[].Characteristics` (e.g. date pickers, dropdowns)
- **ADV-02**: User can navigate from a scenario to the role types it operates on, and from there to live instances of those roles (scenario→role→record cross-linking)
- **ADV-03**: User can visualize a role's relation neighborhood as a graph (relation explorer view)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Relation editing (add/remove) | Relations are created via scenarios in Backlot; a UI editor fights the domain model |
| Create role from scratch | Roles are born from scenarios, not hand-crafted; bypasses domain rules |
| Running / executing scenarios from Studio | Scalar side panel handles this; avoid turning Studio into an execution console |
| Bulk edit / bulk delete | High blast radius on domain state; not validated as needed for v1 |
| Mobile-responsive layout | Management tooling is desktop-used; deferred to avoid competing with core scope |
| Real-time / live-updating lists | No validated need; Turbo navigation provides fresh data on each visit |
| Raw JSON blob editor for roles | Loses schema/validation/permission awareness; superseded by schema-driven form |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 1 | Pending |
| AUTH-02 | Phase 1 | Pending |
| AUTH-03 | Phase 1 | Pending |
| AUTH-04 | Phase 1 | Pending |
| SCEN-01 | Phase 2 | Pending |
| SCEN-02 | Phase 2 | Pending |
| ROLE-01 | Phase 3 | Pending |
| ROLE-02 | Phase 3 | Pending |
| ROLE-03 | Phase 3 | Pending |
| DETL-01 | Phase 3 | Pending |
| DETL-02 | Phase 3 | Pending |
| DETL-03 | Phase 3 | Pending |
| EDIT-01 | Phase 4 | Pending |
| EDIT-02 | Phase 4 | Pending |
| EDIT-03 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 15 total
- Mapped to phases: 15
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-21*
*Last updated: 2026-06-21 after roadmap creation (phase mappings added)*
