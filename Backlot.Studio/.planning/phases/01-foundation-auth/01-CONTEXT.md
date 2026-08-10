# Phase 1: Foundation & Auth - Context

**Gathered:** 2026-06-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Project scaffold, typed API service layer, and session-based Basic Auth boundary that gates everything. A user can log in to Studio and reach an authenticated shell, with all API access flowing through a typed `HttpClient` that injects session-held Basic Auth credentials and handles auth failures cleanly. This is the foundation all subsequent phases build on.

</domain>

<decisions>
## Implementation Decisions

### Navigation Shell
- **D-01:** Use a **left sidebar** layout (not top navbar). Two-panel: fixed sidebar + main content area via Bootstrap flex.
- **D-02:** Sidebar contains **placeholder nav items** (disabled/grayed links) for Scenarios and Roles in Phase 1, before those pages exist. Each phase activates its own nav entry when it ships.
- **D-03:** Sidebar is **collapsible** via a toggle button (icon-only collapsed mode). JS needed to manage toggle state; full width collapses to icon rail.

### Session Lifetime
- **D-04:** Session idle timeout is **8 hours** (matches a workday for a developer tool used at a desk).
- **D-05:** Session uses **sliding expiry** — timeout resets on each request. Active use stays logged in; idle for 8 hours triggers re-login.

### Login Page
- **D-06:** Login page is a **centered Bootstrap card** with Studio branding (app title at top, username + password fields, login button). Full-page centered layout — no sidebar visible on the login page.
- **D-07:** Wrong-credentials error is shown as a **Bootstrap `alert-danger` banner above the form** with the message "Invalid username or password." No field-level highlighting (Basic Auth does not indicate which field is wrong).

### Navbar Identity
- **D-08:** Authenticated user identity is shown **in the sidebar** (top or bottom section), displaying **username only** with a logout link beneath it. The top horizontal bar stays clean (no identity info there).
- **D-09:** Only the **username** is shown — no role/type label. Data source: `GET /api/role/director/whoami`.

### Claude's Discretion
- Exact sidebar collapse animation (CSS transition duration, icon used for toggle).
- Whether sidebar toggle state persists across Turbo navigations (use `data-turbo-permanent` on the sidebar element to preserve state).
- Bootstrap color theme (light/dark mode default).
- Exact wording of the "Backlot Studio" branding on the login card.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Constraints & Architecture
- `.planning/PROJECT.md` — Core value, constraints (Razor Pages + Turbo + Bootstrap, no SPA), auth model, key decisions
- `.planning/REQUIREMENTS.md` — AUTH-01 through AUTH-04 (the 4 requirements this phase must satisfy)
- `.planning/ROADMAP.md` §Phase 1 — Goal, success criteria, and plan breakdown (01-01, 01-02, 01-03)

### Tech Stack Decisions (from CLAUDE.md)
- `.claude/CLAUDE.md` — Stack doc with pinned versions (Bootstrap 5.3.8, Turbo 8.0.23, Scalar 1.60.0, .NET 10), CDN embed patterns, auth pattern (typed `HttpClient` + `DelegatingHandler`), session config pattern (`AddDistributedMemoryCache` + `AddSession` + cookie auth), and "What NOT to Use" list

### API Endpoints for Phase 1
- `openapidoc.json` — OpenAPI spec; relevant endpoints:
  - `GET /api/role/director/isauthenticated` — check session validity
  - `GET /api/role/director/whoami` — current user identity for navbar

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `openapidoc.json` (root of `Backlot.Studio/`) — OpenAPI spec file already present. Served statically in Phase 1 for Scalar (Phase 2 will use it).

### Established Patterns
- No existing code yet — greenfield project. The `.csproj`, `Program.cs`, and `_Layout.cshtml` are all created in Plan 01-01.
- Pattern for typed client + auth handler is defined in `.claude/CLAUDE.md` §Architecture-Critical Patterns — use that as the implementation guide.

### Integration Points
- The Backlot API is a separate process; Studio connects via configured `BacklotApi:BaseUrl` (default `https://localhost:7221`).
- `BasicAuthHandler : DelegatingHandler` reads credentials from `ISession` per request — this is the single point where session creds become API headers.
- `[Authorize]` on all PageModels except `/Login` drives the cookie auth redirect to `/Login` on unauthenticated access.
- 401 from the API (expired/invalid creds mid-session) → top-level redirect to `/Login` via middleware or the service layer — NOT inside a Turbo Frame.

</code_context>

<specifics>
## Specific Ideas

- The sidebar toggle should use Bootstrap Icons for the hamburger/arrow icon (Bootstrap Icons 1.13.1 is already in the recommended stack — CDN CSS).
- Use `data-turbo-permanent` on the sidebar element so the collapse state survives Turbo Drive navigations across pages.
- The login card should include the text "Backlot Studio" as the heading — keep it utilitarian, no custom logo needed for v1.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 1-Foundation & Auth*
*Context gathered: 2026-06-21*
