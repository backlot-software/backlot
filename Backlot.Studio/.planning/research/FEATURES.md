# Feature Research

**Domain:** Developer-facing API management frontend (admin panel / entity explorer over the Backlot API)
**Researched:** 2026-06-21
**Confidence:** MEDIUM

> Confidence note: Findings are corroborated across multiple established products (Forest Admin, Retool, Django Admin, Strapi, Directus, Swagger UI, Postman, Scalar, Backstage) and against the concrete Backlot OpenAPI surface. Sources are web/secondary (the provider seam classifies single-source web as LOW); cross-corroboration plus the hard API contract raises practical confidence to MEDIUM. Feature *categorization* is opinionated and grounded in the locked PROJECT.md scope.

## Feature Landscape

This product sits at the intersection of two well-established UI categories:

1. **Entity/record admin panels** (Forest Admin, Retool, Django Admin, Strapi, Directus) — browse, search, view, edit persisted records and their relations.
2. **API explorer / developer portals** (Swagger UI, Postman, Scalar, Backstage) — browse and inspect API operations.

Backlot.Studio is the **management half** with a thin API-explorer overlay (the Scalar side panel). The role/entity management surface is the core; the scenario browser plus Scalar panel is the API-explorer surface.

### Table Stakes (Users Expect These)

Missing any of these makes the tool feel broken to a developer/operator.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Authentication + redirect to login | No management tool ships unauthenticated; Basic Auth is the project's chosen scheme | LOW | Server-side session holds base64 `username:password`; check via `GET /director/isauthenticated`; redirect unauthenticated users to login |
| Logout / session expiry handling | Users expect to end a session and to be re-prompted when the session dies | LOW | Clear server session; on 401 from API, bounce to login |
| Entity (role) list with pagination | Every admin panel lists records page-by-page; backend already paginates | LOW | `POST /simplequery/find` returns `{Page, PageSize, Total, Results}`. Default page size 25 (research consensus 10-25, 25 optimal) |
| Search / filter on the list | Finding "any role in the system" is the stated Core Value; impossible without search | MEDIUM | `find` supports `Criteria[]` (Field/Condition/Value), `OrderBy`, `From`/`Till`. Show result count; provide a "clear" affordance |
| Record (role) detail view | Inspecting state is half the Core Value; raw fields + metadata must be visible | MEDIUM | `POST /seekbase/detail`. Render dynamic/unknown fields generically; surface `__Permission` and `__Skills` |
| Related-records view with navigation | Project explicitly requires viewing relations and navigating to them | MEDIUM | `POST /persist/relations` returns `[{Uid, Info}]`; render as clickable links into each related role's detail page |
| Edit record on a dedicated route | Editing is the third pillar of Core Value; `/roles/:uid/edit` is a locked requirement | MEDIUM-HIGH | `POST /persist/persist` saves. Form must be generated from the role's field schema (`GET /director/roles` Fields[]) since roles are heterogeneous |
| Server-side validation feedback | Developers expect to see why a save failed, not a silent error | MEDIUM | `POST /role/isvalid` validates before/independent of persist; surface field-level messages |
| Scenario overview list | Browsing scenarios is a locked requirement and the "what can this system do" view | LOW | `GET /director/scenarios` returns Scenario/Result/Roles/Tags/Endpoints/Configurations |
| Scalar API reference panel | Locked requirement; the API-explorer surface developers expect | LOW-MEDIUM | `@scalar/api-reference` via CDN, slide-in side panel keyed to the selected scenario |
| Current-user indicator ("who am I") | Operators need to know which identity/permissions they're acting under | LOW | `GET /director/whoami`; show username + a permission hint in the chrome |
| Empty / loading / error states | A management tool that shows blank or spins forever reads as broken | LOW-MEDIUM | Per-view skeletons, empty-list messaging, API-error surfacing. Easy to forget, high perceived-quality impact |
| Clear navigation / IA (roles vs scenarios) | Two top-level concepts must be obviously separated | LOW | Bootstrap navbar/sidebar; TurboJS for SPA-like transitions without a JS framework |

### Differentiators (Competitive Advantage)

Not required for a working tool, but where Studio can feel purpose-built for Backlot rather than a generic admin scaffold.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Schema-aware dynamic forms from `Characteristics` | Backlot's `Fields[].Characteristics` carry parameterized metadata (e.g. validation, widget hints); rendering inputs from them beats a generic JSON blob editor | HIGH | This is the Backlot-native edge — most generic panels can't model MPP roles this richly. Drives both detail rendering and edit forms |
| Permission-aware UI (read/write gating) | Roles carry `__Permission` (CanCreate/CanRead/CanWrite); hiding/disabling Edit when `CanWrite=false` prevents dead-end actions | MEDIUM | Reads directly off the persist/detail payload; small effort, big UX clarity |
| Skills / role-marker visualization | `__Skills` (Role, Permission, Uid, Persist...) explain *why* a role behaves as it does; surfacing them teaches the MPP model | LOW-MEDIUM | Badge/chip display on detail page; uniquely meaningful in the Backlot ecosystem |
| Scenario → role → record cross-linking | From a scenario, jump to the roles it operates on, then to live records of that role type — ties the two surfaces together | MEDIUM | Scenarios expose `Roles[]`; link into the filtered role list. This is the "Backstage catalog" feeling, scoped to Backlot |
| Deep-linkable detail/edit URLs | `/roles/:uid` and `/roles/:uid/edit` shareable between developers (mirrors API-explorer deep-linking) | LOW | Already implied by the route design; ensure URLs are bookmarkable and Turbo-navigable |
| Relation graph / explorer view | Forest Admin's "Explorer" pattern — visualize a record's relation neighborhood | HIGH | Strong demo value but heavy; defer unless validated (see Anti-Features / v2) |
| Inline copy-to-clipboard for UIDs | UIDs are long opaque strings developers paste into API calls; one-click copy is a small delight | LOW | Cheap polish that operators notice immediately |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Relation editing (add/remove) in v1 | Feels like the natural completion of "view relations" | Explicitly out of scope; relations are created via scenarios in Backlot, so a UI editor competes with the domain model and risks inconsistent state | View + navigate only for v1; create relations by running the appropriate scenario |
| Create-role-from-scratch forms | Generic admin panels default to full CRUD incl. Create | Backlot roles are *born from scenarios*, not hand-built; a blank Create form bypasses domain rules (the classic "CRUD-as-product" anti-pattern) | Out of scope; document that creation happens via scenarios |
| Revision history UI | The API exposes `seekbase/revisions`, so it looks "free" | Diffing/visualizing revisions is real UI work and not validated as needed; scope creep into a v2 feature | Defer to v2; endpoint exists when the need is proven |
| Running/executing scenarios from Studio | "Browse scenarios" naturally tempts "...and run them" | Turns a read/manage tool into an arbitrary-action console; large surface for permissions, param-building, and destructive mistakes | Use the embedded Scalar panel to *try* requests; keep Studio focused on inspect/manage |
| Generic raw-JSON editor for records | Fastest path to "editing works" | Loses all schema/validation/permission awareness; encourages malformed saves; the over-complex/incoherent-UI anti-pattern | Schema-driven forms from `Fields[]` + `isvalid` validation |
| Client-side storage of credentials | Simplifies the proxy | Defeats the project's security boundary (auth must stay server-side); exposes base64 creds to the browser | Server-side session + Razor proxy (already the locked decision) |
| Mobile-responsive layout in v1 | "Should work everywhere" | Management tooling is desk-used; responsive work competes with core scope | Desktop-first Bootstrap; revisit later |
| Real-time/live-updating lists | Feels modern | Adds polling/websocket complexity with no validated need for a manage-on-demand tool | Manual refresh; reload-on-navigate via Turbo |
| Bulk edit / bulk delete | Standard in mature admin panels | High blast radius on persisted domain state; not in Core Value | Single-record edit only for v1 |

## Feature Dependencies

```
Authentication (login + session)
    └──requires──> Server-side API proxy (auth boundary)
            ├──gates──> Role list (find)
            │               ├──requires──> Search/filter + pagination
            │               └──requires──> Role schema (GET /director/roles  Fields[])
            │                                   └──enables──> Schema-driven detail view
            │                                                     ├──enables──> Schema-driven edit form
            │                                                     │                 └──requires──> isvalid + persist
            │                                                     ├──requires──> Relations view (persist/relations)
            │                                                     │                 └──enhances──> cross-link navigation
            │                                                     └──surfaces──> __Permission / __Skills display
            └──gates──> Scenario overview (director/scenarios)
                            ├──enhances──> Scalar API panel (CDN)
                            └──enhances──> Scenario→role→record cross-linking

whoami ──enhances──> permission-aware UI
Relation editing ──conflicts──> "relations created via scenarios" domain rule (excluded)
Create-from-scratch ──conflicts──> "roles born from scenarios" domain rule (excluded)
```

### Dependency Notes

- **Everything requires the server-side proxy + auth:** Every data view calls the Backlot API, and the locked decision is that credentials never reach the browser. The proxy is the foundational layer; build it first.
- **Detail and edit views require the role schema:** Roles are heterogeneous. `GET /director/roles` (Fields + Characteristics) is what lets Studio render the right inputs instead of a generic blob. The schema fetch is a prerequisite for any schema-aware rendering.
- **Edit requires isvalid + persist together:** Saving without validation feedback is a known frustration; treat `role/isvalid` and `persist/persist` as one feature.
- **Relations view enhances (does not require) cross-linking:** You can show relations as flat `Info` strings first, then upgrade to clickable navigation once detail routing exists.
- **Relation editing & create-from-scratch conflict with the domain model:** Both are excluded by design, not just deferred — they fight Backlot's "scenarios mutate state" principle.

## MVP Definition

### Launch With (v1) — matches locked PROJECT.md requirements

- [ ] Basic Auth login + redirect-to-login when unauthenticated — security gate for everything
- [ ] Server-side API proxy with session-held credentials — the auth boundary
- [ ] Scenario overview list (`director/scenarios`) — "what can the system do"
- [ ] Scalar API reference side panel per scenario — the API-explorer surface
- [ ] Role list with search + pagination (`simplequery/find`) — the discovery surface (Core Value)
- [ ] Role detail view with fields, permissions, skills (`seekbase/detail`) — inspect state (Core Value)
- [ ] Related-roles view with navigation (`persist/relations`) — inspect relations (Core Value)
- [ ] Role edit on `/roles/:uid/edit` with validation + persist — mutate state (Core Value)
- [ ] Loading / empty / error states across all views — baseline perceived quality

### Add After Validation (v1.x)

- [ ] Schema-aware dynamic forms from `Characteristics` — once basic edit is proven, deepen field rendering (trigger: users hit fields the generic form can't represent)
- [ ] Permission-aware action gating (hide/disable Edit on `CanWrite=false`) — trigger: users report dead-end edit attempts
- [ ] Scenario → role → record cross-linking — trigger: users manually re-search after viewing a scenario
- [ ] Copy-to-clipboard for UIDs and quality-of-life polish — trigger: observed copy/paste friction
- [ ] whoami in chrome + current-permission hint — trigger: confusion about acting identity

### Future Consideration (v2+)

- [ ] Revision history UI (`seekbase/revisions`) — defer: not validated; non-trivial diff UI
- [ ] Relation graph / Explorer view — defer: high cost, demo-grade value, needs validation
- [ ] Relation editing — defer/avoid: conflicts with domain model; only if scenario-based creation proves insufficient
- [ ] Mobile-responsive layout — defer: desktop-first is intentional for desk-used tooling

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Auth + login redirect + server proxy | HIGH | LOW | P1 |
| Role list + search + pagination | HIGH | MEDIUM | P1 |
| Role detail (fields/permissions/skills) | HIGH | MEDIUM | P1 |
| Related-roles view + navigation | HIGH | MEDIUM | P1 |
| Role edit + validation + persist | HIGH | MEDIUM-HIGH | P1 |
| Scenario overview list | MEDIUM | LOW | P1 |
| Scalar API side panel | MEDIUM | LOW-MEDIUM | P1 |
| Loading/empty/error states | MEDIUM | LOW-MEDIUM | P1 |
| Schema-aware dynamic forms (Characteristics) | HIGH | HIGH | P2 |
| Permission-aware action gating | MEDIUM | MEDIUM | P2 |
| Scenario↔role↔record cross-linking | MEDIUM | MEDIUM | P2 |
| Copy UID / QoL polish | LOW | LOW | P2 |
| whoami chrome indicator | LOW | LOW | P2 |
| Revision history UI | MEDIUM | HIGH | P3 |
| Relation graph / Explorer | MEDIUM | HIGH | P3 |
| Mobile responsiveness | LOW | MEDIUM | P3 |

**Priority key:**
- P1: Must have for launch (maps 1:1 to locked PROJECT.md Active requirements)
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | Forest Admin / Django Admin | Swagger UI / Scalar / Backstage | Our Approach |
|---------|-----------------------------|---------------------------------|--------------|
| Record list + search + paginate | Core CRUD list, auto from schema | n/a | Same, driven by `simplequery/find` |
| Record detail (summary + raw) | Forest: Summary/Details split | n/a | Single detail page: fields + `__Permission` + `__Skills` |
| Relation exploration | Forest "Explorer", related-data cards | n/a | View + navigate (no edit); graph deferred to v2 |
| Edit form from schema | Auto-generated from model fields | n/a | Generated from `director/roles` Fields + Characteristics |
| API operation browsing | n/a | Interactive OpenAPI reference, deep-links | Scalar side panel + scenario list |
| Operation try/run | n/a | Fire test requests in-page | Via Scalar panel only; Studio does not execute scenarios |
| Permissions | Granular RBAC governance | Auth headers | Surface role `__Permission`; gate UI actions (v1.x) |
| Create from scratch | Standard Create form | n/a | Deliberately excluded — roles created via scenarios |

## Sources

- [Forest Admin — Django Admin Alternative (record Summary/Details/Explorer/Analytics)](https://dev.to/forestadmin/forest-admin-django-admin-alternative-3olb)
- [Retool — admin panels use case](https://retool.com/use-case/admin-panels)
- [Retool — upgrade from Django Admin](https://retool.com/use-case/django-admin-panel)
- [Directus — features from other internal tools (discussion)](https://github.com/directus/directus/discussions/10443)
- [Top admin panel builder tools 2026 (WeWeb)](https://www.weweb.io/blog/best-admin-panel-builder-tools)
- [Swagger UI — interactive API reference](https://swagger.io/tools/swagger-ui/)
- [Backstage API Docs plugin (Roadie)](https://roadie.io/backstage/plugins/api-docs/)
- [ServiceStack API Explorer](https://docs.servicestack.net/api-explorer)
- [9 Best Swagger alternatives 2026 (Postman/Scalar)](https://docsio.co/blog/swagger-alternative)
- [Designing effective data table UI (Justinmind)](https://www.justinmind.com/ui-design/data-table)
- [Data Table UX patterns & best practices (Pencil & Paper)](https://www.pencilandpaper.io/articles/ux-pattern-analysis-enterprise-data-tables)
- [Filter UX design patterns (Pencil & Paper)](https://www.pencilandpaper.io/articles/ux-pattern-analysis-enterprise-filtering)
- [Why pagination matters in table design (Alf Design Group)](https://www.alfdesigngroup.com/post/why-pagination-is-important-for-table-design)
- [User Interface Anti-Patterns (ui-patterns.com)](https://ui-patterns.com/blog/User-Interface-AntiPatterns)
- [Common anti-patterns in web applications (Three Dots Labs)](https://threedots.tech/post/common-anti-patterns-in-go-web-applications/)
- Backlot OpenAPI spec (`openapidoc.json`) and PROJECT.md (primary, project-internal)

---
*Feature research for: developer-facing API management frontend (Backlot.Studio)*
*Researched: 2026-06-21*
