# Backlot.Studio

## What This Is

Backlot.Studio is a standalone .NET Razor Pages web application that serves as the management frontend for the Backlot API. It gives developers and operators a visual interface to browse all registered scenarios, manage persisted roles (search, view details, edit), and inspect role relations — all backed by the Backlot API running alongside it.

## Core Value

A developer or operator can find any role in the system, inspect its state and relations, and edit it — without writing a single API call by hand.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] User can log in with username and password (Basic Auth: base64 of `username:password`)
- [ ] User is redirected to login when not authenticated
- [ ] User can view an overview of all registered scenarios
- [ ] User can open a Scalar API reference side panel for any scenario
- [ ] User can browse all roles with search and pagination
- [ ] User can view a role detail page (fields, permissions, skills)
- [ ] User can view related roles on the detail page and navigate to their detail pages
- [ ] User can edit a role on a dedicated edit route (`/roles/:uid/edit`)

### Out of Scope

- Managing relations (add/remove) — view and navigate only for v1
- Role creation from scratch — roles are created via scenarios
- Revisions history UI — deferred to v2
- Mobile-optimized design — desktop-first for v1

## Context

- The Backlot API is a separate process (configured base URL, default `https://localhost:7221`)
- Authentication is HTTP Basic Auth: credentials are base64-encoded `username:password` and sent as the `Authorization` header on every API request
- The OpenAPI spec (`openapidoc.json`) describes all available endpoints; key ones for Studio:
  - `GET /api/role/director/scenarios` — list all scenarios
  - `GET /api/role/director/roles` — list all role types
  - `POST /api/role/simplequery/find` — search/paginate roles
  - `POST /api/role/seekbase/detail` — get a role's full detail by UID
  - `POST /api/role/persist/persist` — save/update a role
  - `POST /api/role/persist/relations` — get related roles for a UID
  - `GET /api/role/director/isauthenticated` — check session validity
  - `GET /api/role/director/whoami` — get current user info
- Scalar API reference is embedded as a side panel (slide-in overlay) powered by `@scalar/api-reference` loaded via CDN script tag
- Credentials are stored server-side in ASP.NET session; Razor Pages proxy API requests to avoid exposing auth to the browser

## Constraints

- **Tech Stack**: .NET Razor Pages + TurboJS + Bootstrap — no React/Vue/SPA framework
- **Project**: Standalone `.csproj` inside `Backlot.Studio/`, added to `Backlot.sln`
- **API**: All data comes from the Backlot API; Studio has no database of its own
- **Auth**: Basic Auth only — username/password encoded as base64, sent on every request

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Server-side API proxying | Keeps auth credentials out of the browser; cleaner security boundary | — Pending |
| TurboJS for navigation | SPA-like UX without a JS framework build pipeline | — Pending |
| Scalar side panel via CDN | Zero build-step integration, Scalar handles the rendering | — Pending |
| Desktop-first Bootstrap layout | Management tooling is used at a desk; mobile deferred | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-21 after initialization*
