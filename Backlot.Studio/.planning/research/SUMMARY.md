# Project Research Summary

**Project:** Backlot.Studio
**Domain:** .NET Razor Pages admin/management frontend (server-rendered, Turbo-enhanced) acting as a session-auth proxy/BFF over the Backlot REST API
**Researched:** 2026-06-21
**Confidence:** MEDIUM-HIGH

## Executive Summary

Backlot.Studio is a **developer-facing management frontend** — an entity/record admin panel (the Forest Admin / Django Admin / Retool family) with a thin API-explorer overlay (the Swagger UI / Scalar family). The core surface lets a developer or operator find any persisted role, inspect its state/permissions/skills/relations, and edit it; the secondary surface lists scenarios and embeds a Scalar OpenAPI reference. The expert way to build this is as a **thin presentation layer that owns no data**: every page renders server-side from data fetched from the Backlot API at request time, with the user's Basic Auth credentials held in server-side session and injected into every outbound call. There is no SPA framework and no JS build pipeline — Hotwired Turbo provides SPA-like navigation over plain Razor Pages, and Bootstrap/Scalar load via pinned CDN tags.

The recommended stack is fully in-box and version-verified: **.NET 10 / ASP.NET Core 10 Razor Pages**, `IHttpClientFactory` typed clients with a Basic Auth `DelegatingHandler`, ASP.NET session + cookie auth, **Hotwired Turbo 8.0.23**, **Bootstrap 5.3.8**, and **Scalar API Reference 1.60.0** (all CDN, pinned). The dependency story is clean and linear: a server-side API proxy with session-held credentials is the foundation that gates every other feature, and the role schema (`GET /director/roles` Fields/Characteristics) is the prerequisite for any schema-aware detail/edit rendering. Architecture research gives a clear, dependency-driven build order (skeleton -> service layer -> auth/session -> read pages -> list -> detail -> edit -> Scalar) that should map almost 1:1 onto roadmap phases.

The dominant risks are not in the stack but in the **Turbo + server-rendered + third-party-JS seam**, and they are well-documented. The top hazards: Razor POST handlers returning HTTP 200 (Turbo silently swallows success and validation errors — must use 303-on-success / 422-on-invalid); Scalar/CDN scripts initialized once on full load and breaking after Turbo navigations (must init on `turbo:load`, tear down, and pin versions); and credentials living in session with no absolute expiry or upstream revalidation (must add idle timeout + `isauthenticated` revalidation + clean 401->top-level redirect). The key scope discipline from feature research: roles are **born from scenarios**, so create-from-scratch forms and relation editing are deliberately excluded, not just deferred — building them would fight the domain model.

## Key Findings

### Recommended Stack

The entire stack is delivered by the .NET 10 shared framework plus three pinned CDN front-end libraries — no npm, no `package.json`, no build step (intentional per project constraints). Front-end interactivity comes from Turbo hijacking links/forms; styling and dark-mode from Bootstrap 5.3; the embedded API reference from Scalar. Confidence on versions is HIGH (verified against the npm registry and Microsoft docs); confidence on Turbo+Razor integration specifics is MEDIUM. See `STACK.md`.

**Core technologies:**
- **.NET 10 / ASP.NET Core 10 Razor Pages** (LTS): page-per-route server-rendered UI — matches the Backlot framework target; page-centric model fits "one screen = one page" admin tooling better than MVC/Minimal APIs.
- **Hotwired Turbo 8.0.23** (CDN): SPA-like navigation (Drive), partial updates (Frames), targeted DOM swaps (Streams) — delivers SPA feel with zero JS build pipeline (the stated goal).
- **Bootstrap 5.3.8** + **Bootstrap Icons 1.13.1** (CDN): layout, components, built-in dark/light color-mode — constraint-mandated, CDN-deliverable.
- **Scalar API Reference 1.60.0** (CDN): embedded OpenAPI reference side panel via `Scalar.createApiReference()` — renders `openapidoc.json` with no build integration.
- **`IHttpClientFactory` typed clients + Basic Auth `DelegatingHandler`** (in-box): the canonical, pooled way Studio talks to the API and injects credentials — never `new HttpClient()`.
- **ASP.NET Session + Cookie Auth + System.Text.Json** (in-box): server-side credential storage (creds never reach the browser) and `[Authorize]`/`LoginPath` gating.

### Expected Features

Studio's feature set is well-bounded and maps 1:1 to the locked PROJECT.md requirements. The "manage" surface (role list/detail/relations/edit) is the core; the "explore" surface (scenario list + Scalar) is the overlay. Feature confidence is MEDIUM — cross-corroborated across many established admin/API-explorer products and anchored to the concrete Backlot OpenAPI contract. See `FEATURES.md`.

**Must have (table stakes):**
- Basic Auth login + redirect-to-login when unauthenticated — security gate for everything
- Server-side API proxy with session-held credentials — the auth boundary
- Role list with search/filter + server-side pagination (`simplequery/find`) — the discovery surface (Core Value)
- Role detail view with fields, `__Permission`, `__Skills` (`seekbase/detail`) — inspect state (Core Value)
- Related-roles view with clickable navigation (`persist/relations`) — inspect relations (Core Value)
- Role edit on `/roles/:uid/edit` with validation + persist (`isvalid` + `persist`) — mutate state (Core Value)
- Scenario overview list (`director/scenarios`) + Scalar API reference panel — the explorer surface
- Loading / empty / error states across all views — baseline perceived quality

**Should have (competitive / Backlot-native edge):**
- Schema-aware dynamic forms from `Fields[].Characteristics` — beats a generic JSON blob editor; the Backlot-native differentiator
- Permission-aware UI gating (hide/disable Edit when `CanWrite=false`) — prevents dead-end actions
- Skills/role-marker visualization + scenario->role->record cross-linking — teaches the MPP model and ties the two surfaces together
- Copy-to-clipboard for UIDs, `whoami` chrome indicator — low-cost polish operators notice

**Defer (v2+) / deliberately exclude:**
- Revision history UI, relation graph/explorer, mobile-responsive layout — defer (unvalidated / high cost)
- **Create-role-from-scratch forms and relation editing — exclude by design** (roles are born from scenarios; these fight the domain model)
- Running/executing scenarios from Studio, raw-JSON editor, bulk ops, real-time lists — anti-features

### Architecture Approach

Studio is a stateless thin proxy: Pages orchestrate fetch->bind->render and contain no HTTP/auth logic; a typed-client **Service layer** owns all API calls and unwraps the `{ Body, Status, TimeInMs }` envelope; an **Infrastructure layer** holds the cross-cutting `BasicAuthHandler` (reads session per call), the session/auth gate middleware, and session keys. Shared partials double as Turbo Frame fragments so the same `_RoleTable`/`_RoleDetail` markup serves both full-page composition and frame requests. Confidence is MEDIUM-HIGH (HttpClient/DelegatingHandler patterns HIGH from MS docs; Turbo+Razor integration MEDIUM). See `ARCHITECTURE.md`.

**Major components:**
1. **Pages (PageModel)** — one per route (`/login`, `/scenarios`, `/roles`, `/roles/{uid}`, `/roles/{uid}/edit`); orchestrate fetch->bind->render only.
2. **API Service layer** (`IRoleApi`/`IScenarioApi`/`IAuthApi` + `Envelope<T>`) — typed HttpClient methods; unwrap envelope; role detail body is dynamic -> `Dictionary<string,JsonElement>`.
3. **BasicAuthHandler (DelegatingHandler)** — the only place credentials are read; reads base64 creds from session via `IHttpContextAccessor` on **every** `SendAsync`.
4. **Auth/session + guard middleware** — Data-Protection-backed session holds creds; middleware redirects unauthenticated requests to `/login`.
5. **Scalar side panel** — CDN script in the layout, `data-turbo-permanent`, lives outside Turbo's swap region.

### Critical Pitfalls

The pitfall research is HIGH confidence and concentrates almost entirely in the Turbo seam. The top hazards (see `PITFALLS.md`):

1. **Razor POST returns 200 -> Turbo swallows it** — successful saves don't navigate and validation errors never render. Use `RedirectToPage()` (302/303) on success and `Response.StatusCode = 422` before `return Page()` on validation failure. Build a base PageModel `InvalidPage()` helper.
2. **Scalar/CDN scripts init once on full load** — panel blanks/duplicates/goes stale after Turbo navigation. Initialize inside a `turbo:load` handler, tear down prior instances, keep the mount in a `data-turbo-permanent` container, and pin the CDN version (never `@latest`).
3. **Credentials in session with no expiry/revocation** — users never get logged out; disabled API accounts still work. Set absolute `IdleTimeout`, `HttpOnly`/`Secure`/`SameSite` cookie flags, revalidate via `GET /director/isauthenticated`, and provide explicit logout (`Session.Clear()`).
4. **401-driven redirect fights Turbo Frames** — expired-session login renders inside a frame or errors. Centralize 401 handling and break out to `_top` / full Turbo visit on auth failure.
5. **HTTP N+1 + offset pagination on the list** — per-row detail fetches and deep offsets hammer the API. Render the list from the `find` response alone, page server-side, debounce search; enrich only on the detail page (and fire detail + relations in parallel with `Task.WhenAll`).
6. **Antiforgery token stale under Turbo** — intermittent 400s on save after a Turbo nav. Use tag-helper forms; for custom fetches read a `<meta name="csrf-token">`; test save *after* a Turbo navigation, not just hard load.

## Implications for Roadmap

The architecture research provides a dependency-driven build order that translates almost directly into phases. The critical path is **skeleton -> service layer -> auth/session**, which gates everything; Turbo Frame conventions are established at the list phase and reused for detail/edit; Scalar is additive and can come last (or be parallelized).

### Phase 1: Project skeleton + shell + service layer
**Rationale:** Everything depends on the project existing, the Bootstrap/Turbo layout, and a typed-client service layer that can call the API and unwrap the envelope. The service layer can be built and unit-tested against the API before any auth UI exists.
**Delivers:** `Backlot.Studio.csproj` added to the solution, `Program.cs` DI wiring, `_Layout.cshtml` (Bootstrap + pinned Turbo CDN), `IRoleApi`/`IScenarioApi`/`IAuthApi`, `Envelope<T>`, config (`Backlot:BaseUrl`).
**Uses:** .NET 10 Razor Pages, Bootstrap 5.3.8, Turbo 8.0.23, `IHttpClientFactory` typed clients, System.Text.Json.
**Avoids:** `new HttpClient()` per request (Anti-Pattern 4); HTTP/auth logic in PageModels; `@latest` CDN pins; Bootstrap JS dead after Turbo nav (re-init on `turbo:load`).

### Phase 2: Auth / session-proxy foundation
**Rationale:** The auth boundary gates every data view; login validates through the service layer via `isauthenticated`. This is the highest-risk security surface and must establish the canonical contracts before any protected page.
**Delivers:** Login page, `BasicAuthHandler` (reads session per call), session config (HttpOnly/Secure/SameSite, absolute `IdleTimeout`), auth-guard middleware, logout, and the canonical **401 -> top-level redirect** contract.
**Implements:** BasicAuthHandler + Auth/session components.
**Avoids:** Credentials in session without expiry/revocation (Pitfall 3); 401 fighting Turbo Frames (Pitfall 4); caching creds/HttpContext in the handler (Anti-Pattern 1); logging the Authorization header.

### Phase 3: Scenario overview page
**Rationale:** First read-only page; proves end-to-end auth + fetch + render with no Turbo Frames yet — the simplest vertical slice.
**Delivers:** `/scenarios` page from `director/scenarios` (Scenario/Result/Roles/Tags/Endpoints), plus loading/empty/error states.
**Addresses:** Scenario overview list (table stakes).

### Phase 4: Roles list (search + pagination)
**Rationale:** The discovery surface and Core Value entry point; introduces and establishes the Turbo Frame convention reused by later phases.
**Delivers:** `/roles` with search/filter, server-side pagination (`simplequery/find`), result count, and Turbo Frame in-place updates.
**Addresses:** Role list + search + pagination (table stakes).
**Avoids:** HTTP N+1 + offset pagination (Pitfall 5); missing search debounce; in-memory client-side filtering.

### Phase 5: Role detail + relations
**Rationale:** Reuses list-phase patterns; inspecting state and relations is two-thirds of Core Value. Detail is the prerequisite for edit.
**Delivers:** `/roles/{uid}` rendering dynamic fields + `__Permission` + `__Skills` (`seekbase/detail`), and a lazy Turbo Frame for related roles as clickable links (`persist/relations`).
**Addresses:** Role detail view + related-roles navigation (table stakes); skills/permission visualization.
**Avoids:** Serial detail+relations round-trips (fire in parallel / lazy frame); dead-end relation links.

### Phase 6: Role edit
**Rationale:** The only form-POST flow in v1 and the final Core Value pillar; reuses detail fetch/bind. Concentrates the Turbo form hazards in one place.
**Delivers:** `/roles/{uid}/edit` with a schema-driven form, `role/isvalid` validation feedback, `persist/persist` save, antiforgery, and 303-on-success / 422-on-invalid handling (base helper).
**Addresses:** Role edit + validation + persist (table stakes).
**Avoids:** 200-response breaks Turbo forms (Pitfall 1); antiforgery stale under Turbo (Pitfall 6).

### Phase 7: Scalar API reference side panel
**Rationale:** Additive overlay, safest last due to the Turbo-permanent interaction; independent of the data pages so it could also be parallelized after Phase 1.
**Delivers:** Slide-in Scalar panel keyed to the selected scenario, served `openapidoc.json` from Studio, initialized on `turbo:load` with teardown, pinned version.
**Addresses:** Scalar API reference panel (table stakes).
**Avoids:** Scalar breaking on Turbo navigation (Pitfall 2); `@latest` CDN.

### Phase Ordering Rationale
- **Dependency-driven:** Phases 1->2 are the hard critical path (skeleton + service layer + auth gate everything). Detail (5) requires the list/Frame patterns from (4); edit (6) reuses detail's fetch/bind from (5).
- **Risk front-loading:** The security surface (auth/session, credential handling, 401 contract) is settled in Phase 2 before any feature builds on it.
- **Pattern reuse:** Turbo Frame conventions are established once in Phase 4 and reused in 5-6, so the Turbo learning cost is paid early on the lowest-risk data page.
- **Isolation of the riskiest integration:** Scalar (the most fragile Turbo + third-party-JS seam) is isolated last so its breakage can't destabilize core flows.
- **Scope discipline baked in:** No phase introduces create-from-scratch or relation editing — these conflict with Backlot's "scenarios mutate state" model and are excluded by design.

### Research Flags

Phases likely needing deeper research during planning (`/gsd-plan-phase --research-phase <N>`):
- **Phase 6 (Role edit):** Highest-complexity v1 phase — schema-driven form generation from `Characteristics` plus the 303/422/antiforgery Turbo interaction. The schema-aware dynamic form is flagged HIGH complexity in FEATURES.md; consider a thin generic form for v1 and deepen post-validation.
- **Phase 7 (Scalar panel):** Turbo + third-party-JS lifecycle is the single most common bug class here; `turbo:load` re-init, teardown, and `data-turbo-permanent` placement warrant a focused spike against the real CDN build.

Phases with standard, well-documented patterns (skip research-phase):
- **Phase 1 (skeleton + service layer):** Canonical `IHttpClientFactory`/typed-client pattern, HIGH-confidence MS docs.
- **Phase 2 (auth/session):** Standard ASP.NET session + cookie auth + DelegatingHandler; well-documented (the *contracts* matter more than novelty).
- **Phase 3 (scenarios) / Phase 4 (roles list):** Conventional Razor Pages read/list + standard Turbo Frame; established patterns.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Versions verified against npm registry + Microsoft docs; integration specifics MEDIUM |
| Features | MEDIUM | Cross-corroborated across many products + anchored to the concrete Backlot OpenAPI contract; categorization opinionated but grounded in locked PROJECT.md |
| Architecture | MEDIUM-HIGH | HttpClient/DelegatingHandler HIGH (MS docs); Turbo+Razor integration MEDIUM (Turbo handbook + community packages) |
| Pitfalls | HIGH | Turbo form/frame status rules, ASP.NET session/cookie behavior, Scalar CDN embedding all confirmed against official docs + maintainer threads |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address
- **Turbo + Razor integration mechanics (303/422, frame headers, antiforgery survival):** MEDIUM confidence from handbook + community packages, not a single authoritative .NET source. Handle by validating against the real app early — establish the 303/422 base helper in Phase 6 and add a Turbo smoke test (invalid submit -> errors visible; valid submit -> navigates; save after a Turbo nav).
- **Scalar `createApiReference()` under Turbo:** Init/teardown lifecycle is MEDIUM. Handle with a focused spike in Phase 7 against the pinned 1.60.0 build before committing the panel design.
- **Schema-aware form rendering from `Characteristics`:** Field metadata richness is the Backlot-native edge but HIGH complexity and partly unknown until exercised against real role schemas. Handle by shipping a generic-but-correct form in v1 and deepening to characteristic-driven widgets once users hit fields the generic form can't represent.
- **Self-hosted vs CDN Turbo / strict-CSP deployment:** STACK flags LibMan as an option if offline/CSP is required. Not needed for v1; revisit only if a deployment constraint surfaces.
- **Distributed session for scale-out:** In-memory session is fine for single-instance v1 but breaks on a second node (creds vanish). Document the limitation; switch to Redis/distributed cache before horizontal scaling.

## Sources

### Primary (HIGH confidence)
- npm registry — Turbo 8.0.23, Bootstrap 5.3.8, Scalar 1.60.0, Bootstrap Icons 1.13.1 (authoritative versions)
- Microsoft Learn / .NET blog — .NET 10 GA (Nov 2025, LTS to Nov 2028), ASP.NET Core 10; IHttpClientFactory typed clients + DelegatingHandler + handler lifetime/pooling
- Microsoft Learn — Cookie authentication & ValidatePrincipal revocation; ASP.NET Core session timeout / IdleTimeout; antiforgery/CSRF
- Turbo Handbook — Drive 303-redirect / 422-validation rule
- Scalar docs / cdnjs — createApiReference() CDN embed, version pinning
- Backlot PROJECT.md, CLAUDE.md, and openapidoc.json — constraints, auth model, API endpoints

### Secondary (MEDIUM confidence)
- Turbo handbook (building: script eval, data-turbo-permanent, frames, 303) + hotwire.io ASP.NET guide + mvdmio.Hotwire.NET — Turbo+Razor integration
- Milan Jovanovic — extending HttpClient with DelegatingHandlers
- Ben Nadel / hotwired-turbo issues #84, #432, #670 / Coorasse — non-2xx form rule, missing-frame behavior
- brokul.dev — authentication cookie lifetime & sliding-expiration guidance
- Forest Admin / Retool / Django Admin / Directus / Swagger UI / Backstage / ServiceStack — admin-panel & API-explorer feature landscape
- Justinmind / Pencil & Paper / Alf Design / ui-patterns / Three Dots Labs — data-table, filter, pagination UX and anti-patterns

### Tertiary (LOW confidence)
- Single-source web findings on feature categorization (raised to MEDIUM via cross-corroboration and the hard API contract) — validate during implementation against real role schemas and the live API

---
*Research completed: 2026-06-21*
*Ready for roadmap: yes*
