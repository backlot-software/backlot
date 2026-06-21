# Pitfalls Research

**Domain:** .NET Razor Pages admin frontend with TurboJS (Hotwire), Bootstrap, server-side Basic Auth session, and REST API proxying (Backlot.Studio)
**Researched:** 2026-06-21
**Confidence:** HIGH (Turbo form/frame status-code rules, ASP.NET session/cookie behavior, Scalar CDN embedding all confirmed against official docs and maintainer issue threads)

## Critical Pitfalls

### Pitfall 1: Razor Pages POST handlers return 200, breaking Turbo Drive form submissions

**What goes wrong:**
The default Razor Pages success pattern returns `Page()` (HTTP 200) after a POST, or returns the same page with `Page()` on validation failure (also 200). Turbo Drive ignores 200-rendered bodies from form POSTs — after a successful submit the page silently does not navigate, and on a validation failure the error messages never render. The form appears to "do nothing."

**Why it happens:**
Turbo Drive deliberately refuses to render a 200 response to a form POST because the browser's native reload-after-POST warning ("Are you sure you want to resubmit?") cannot be replicated. Turbo requires a **303 See Other** redirect on success and a **non-2xx status (422 Unprocessable Content)** on validation failure. Standard ASP.NET MVC/Razor tutorials return `RedirectToPage()` (302, acceptable) on success but `return Page()` (200) on `!ModelState.IsValid` — that 200 is the trap.

**How to avoid:**
- On successful save (the edit-role flow): `return RedirectToPage(...)` — ASP.NET emits 302, which Turbo follows. Prefer explicit 303 where the framework allows it for POST→GET semantics.
- On validation failure: set `Response.StatusCode = 422;` before `return Page();` so Turbo re-renders the form with errors. Wrap this in a small base PageModel helper (e.g. `InvalidPage()`).
- Add a Turbo smoke-test to the edit flow's acceptance criteria: submit invalid data → confirm errors appear; submit valid data → confirm navigation.

**Warning signs:**
Forms that "swallow" submissions, validation summaries that never appear, or a console message about Turbo not rendering the response. Works fine when you disable Turbo or do a full page reload.

**Phase to address:**
Role edit phase (the only form-POST flow in v1). Establish the 303/422 base-class helper here.

---

### Pitfall 2: Scalar (and other CDN scripts) initialized in `<head>`/inline only run on full load, not Turbo navigations

**What goes wrong:**
Scalar's standalone script and any inline `Scalar.createApiReference(...)` initialization runs on the first full page load. After that, Turbo Drive swaps `<body>` content via fetch+morph without re-executing `<head>` scripts the same way a fresh load would, and re-runs inline `<body>` scripts unpredictably. The Scalar panel renders the first time you visit but is blank, duplicated, or stale after navigating between scenarios via Turbo.

**Why it happens:**
Turbo Drive replaces the `<body>` and merges the `<head>`; it does not re-evaluate `<script src>` tags that are unchanged between pages, and inline scripts may execute before the target element exists. Third-party widgets that assume a classic full-page lifecycle break under SPA-style navigation. This is the single most common Turbo + third-party-JS class of bug.

**How to avoid:**
- Initialize Scalar inside a `turbo:load` event handler (fires on initial load AND after every Turbo navigation), not a one-shot `DOMContentLoaded`.
- Before re-initializing, tear down any prior Scalar instance / clear the mount node to avoid duplicate panels.
- For the slide-in side panel specifically, consider keeping the Scalar mount node inside a `data-turbo-permanent` container with a stable `id` so Turbo preserves it across navigations and you only init once.
- Pin the Scalar CDN version (e.g. `@scalar/api-reference@1.x.x` on jsDelivr/cdnjs) rather than `@latest` — unpinned CDN means a Scalar release can break the panel with zero local changes.

**Warning signs:**
Panel works on hard refresh but breaks after clicking around; duplicated Scalar UI; "works the first time only." Console errors about a missing mount element.

**Phase to address:**
Scalar side-panel phase. Make `turbo:load` re-init + version pinning explicit acceptance criteria.

---

### Pitfall 3: Storing Basic Auth credentials in session without absolute expiry or revalidation

**What goes wrong:**
Credentials (base64 `username:password`) are placed in ASP.NET session and proxied on every API call. Sessions use a sliding-expiration cookie by default; if it is set to a long idle timeout the credentials effectively live forever in server memory/distributed cache, and the user stays "logged in" even after their API account is disabled on the Backlot side. There is no logout-on-revocation path.

**Why it happens:**
ASP.NET session is sliding by default and is decoupled from the auth state of the upstream API. Developers treat "credentials in session = logged in" and never reconcile against `GET /api/role/director/isauthenticated`. Session sliding expiration can also be re-issued indefinitely, which is poor security practice.

**How to avoid:**
- Set a sane absolute session/idle timeout (e.g. `IdleTimeout` of 30–60 min) and `Cookie.HttpOnly = true`, `Cookie.SecurePolicy = Always`, `SameSite = Lax/Strict`.
- Revalidate against `GET /api/role/director/isauthenticated` (cheaply, e.g. on protected page load or via middleware), and force logout + redirect to login when the upstream rejects.
- Never log or surface the raw `Authorization` header; encrypt session if a distributed/persisted session store is used (out-of-process session is not encrypted by default — treat the store as sensitive).
- Provide an explicit logout that clears the session (`HttpContext.Session.Clear()` + cookie removal).

**Warning signs:**
Users never get logged out; disabling an API account doesn't lock them out of Studio; credentials visible in logs or a non-encrypted session store; 401s from the API surface as raw error pages instead of a re-login prompt.

**Phase to address:**
Auth/session-proxy foundation phase. Verification = revoked-account test and idle-timeout test.

---

### Pitfall 4: Login redirect implemented as a 401-driven full redirect that fights Turbo

**What goes wrong:**
When the session expires mid-session, the API proxy returns 401. A naive handler returns a redirect to `/login`, but if that 401/redirect happens inside a Turbo Frame or fetch, Turbo either renders the login page *inside the frame* or logs a "no matching turbo-frame" error, leaving the user stuck on a half-broken page instead of a clean login screen.

**Why it happens:**
Turbo Frames look for a `<turbo-frame>` with a matching `id` in the response; a login page has no such frame, so the frame either errors in the console or empties out. Turbo also does not treat an in-frame 401→redirect the way a top-level navigation would.

**How to avoid:**
- For auth failures, force a **full-page** Turbo visit, not a frame update: respond with `Turbo-Visit`/`Turbo.visit(url, {action:"replace"})` semantics, or emit a `Turbo-Location` redirect that targets the top frame (`data-turbo-frame="_top"` on auth-sensitive links/forms).
- Centralize 401 handling in the proxy/middleware so every API call funnels through the same "session dead → top-level redirect to /login" path.
- Don't wrap auth-protected navigations in Turbo Frames where a session can silently expire; or ensure the login response carries a matching frame / `_top` break-out.

**Warning signs:**
Login form appears nested inside a panel, "Response has no matching `<turbo-frame>`" console errors after a session expires, or a blank frame where content used to be.

**Phase to address:**
Auth/session-proxy phase (define the canonical 401 → top-level redirect contract) and revisit in any phase that introduces Turbo Frames.

---

### Pitfall 5: Offset pagination + per-row detail fetches against the API (N+1 over HTTP)

**What goes wrong:**
The roles list uses `POST /api/role/simplequery/find` for a page of results, then fetches detail/relations per row to render columns, producing N extra HTTP round-trips per page. Combined with naive offset pagination (`skip/take`) over a large role set, list pages get slow and hammer the API.

**Why it happens:**
It's the path of least resistance: render the table, then "just fetch" each row's extra data. Studio has no DB of its own, so every enrichment is a network call. Deep offset pagination also degrades on the API side as offsets grow.

**How to avoid:**
- Render the list from the `find` response alone; defer detail/relations to the detail page (`seekbase/detail`, `persist/relations`). Don't enrich rows with per-row API calls.
- Page server-side via the API's own paging params; never fetch all roles and page in memory.
- Debounce search input and pass the query to the API; don't filter a fully-loaded set client-side.
- Cap page size and show a sensible default; expose page size as a bounded option.

**Warning signs:**
List page latency scales with row count; API request log shows a burst of detail calls per list render; "load everything then filter in C#/JS" code.

**Phase to address:**
Roles list/search/pagination phase.

---

### Pitfall 6: Antiforgery token lost or duplicated under Turbo (CSRF failures on edit)

**What goes wrong:**
Razor Pages auto-inject an antiforgery token; Turbo Drive form submissions can fail antiforgery validation if the token in the swapped `<body>` is stale, or POSTs intermittently return 400 "antiforgery token could not be validated."

**Why it happens:**
Turbo morphs the DOM and reuses the page across visits; the hidden `__RequestVerificationToken` from the original render can go stale relative to the cookie, or scripts that re-render forms drop the token. Hotwire's own guidance is to expose the CSRF token via a `<meta>` tag and attach it to fetch/form requests.

**How to avoid:**
- Keep ASP.NET's automatic antiforgery for standard form posts and confirm the token is present in the Turbo-rendered form (it is, as long as you use the tag helper `<form method="post">`).
- If you ever do custom Turbo fetches that POST, read the token from a `<meta name="csrf-token">` and send it as the `RequestVerificationToken` header.
- Add an edit-flow test that submits after a Turbo navigation (not just after a hard load) to catch stale-token cases.

**Warning signs:**
Intermittent 400s on save that only happen after navigating via Turbo (never on first hard load); antiforgery exceptions in logs.

**Phase to address:**
Role edit phase; validate alongside Pitfall 1's 303/422 work.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Use `@latest` for Scalar/Turbo/Bootstrap CDN | No version management | A vendor release silently breaks the panel/nav with no local change | Never for Scalar/Turbo; pin versions |
| Return `Page()` (200) on validation failure | Matches default Razor tutorials | Turbo silently swallows the failure; errors never show | Never with Turbo enabled |
| In-memory session store | Zero setup | Lost on restart, no scale-out; credentials in plain memory | OK for single-instance v1; document it |
| Enrich list rows with per-row API calls | Quick rich tables | HTTP N+1, slow list pages | Never; enrich on detail page only |
| One global `DOMContentLoaded` init for all JS | Simple | Breaks on every Turbo navigation | Never with Turbo; use `turbo:load` |
| Long sliding session timeout | Fewer re-logins | Stale auth, no revocation, security exposure | Never; set absolute timeout |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Turbo Drive + Razor POST | Returning 200 on success/validation-fail | 303/302 redirect on success, 422 on validation fail |
| Turbo Frames + auth redirect | 401→login renders inside frame or errors | Break out to `_top` / full Turbo visit on auth failure |
| Scalar via CDN | One-shot init in `<head>` | Init in `turbo:load`, tear down prior instance, pin version |
| Backlot API proxy | New `HttpClient` per request (socket exhaustion) | `IHttpClientFactory` named/typed client with base URL + auth handler |
| Basic Auth header | Re-encoding credentials per call / logging them | Build header once in a `DelegatingHandler`; never log it |
| Bootstrap JS (dropdowns/modals) | Components dead after Turbo nav | Use Bootstrap data-api that re-binds, or re-init in `turbo:load`; mark persistent UI `data-turbo-permanent` |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Offset pagination over large role sets | List slows as you page deeper | Use API paging params; bounded page size | Thousands of roles |
| HTTP N+1 on list render | Request log burst per page | Render from `find` only; enrich on detail | Any page > a few rows |
| No search debounce | API hammered per keystroke | Debounce 250–300ms before query | Immediately under typing |
| Synchronous chained proxy calls | Detail page slow (detail + relations serially) | Fire detail and relations calls in parallel (`Task.WhenAll`) | Detail page, always |
| Scalar loading full spec eagerly in panel | Panel/page jank on open | Lazy-load Scalar only when the panel is opened | Large OpenAPI spec |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Sending Basic Auth to the browser / storing in JS | Credential theft; defeats the proxy design | Keep credentials server-side in session; proxy all API calls |
| Logging the `Authorization` header | Credentials in logs | Redact auth header in any logging/diagnostics |
| Session cookie without HttpOnly/Secure/SameSite | Session hijack, CSRF | Set `HttpOnly`, `SecurePolicy=Always`, `SameSite` |
| No upstream auth revalidation | Disabled API account still works in Studio | Revalidate via `isauthenticated`; force logout on 401 |
| Unencrypted out-of-process session store | Credentials readable at rest | Encrypt session data or keep single-instance in-memory for v1 |
| Trusting `https://localhost` dev cert blindly in proxy | MITM / silent cert bypass in prod | Validate API TLS; only relax cert checks in Development |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No loading indicator during Turbo/proxy fetches | UI feels frozen on slow API | Use Turbo's progress bar + per-action spinners |
| Validation errors don't render (Pitfall 1) | User can't tell why save failed | 422 + re-rendered form with error summary |
| Scalar panel duplicates/blanks after nav | Confusing, broken-feeling tool | `turbo:load` re-init + teardown |
| Losing scroll/search state on back-nav | User re-types search after viewing detail | Rely on Turbo's cached restoration; keep search in querystring |
| Related-role links don't navigate | Dead-end detail page | Render relations as real links to `/roles/:uid` |
| Edit form loses unsaved data on accidental nav | Frustration, lost work | Warn on dirty-form navigation (`turbo:before-visit`) |

## "Looks Done But Isn't" Checklist

- [ ] **Edit form:** Often missing 422-on-invalid — verify invalid submit shows errors (not silent) under Turbo.
- [ ] **Edit form:** Often missing antiforgery survival across Turbo nav — verify save works after navigating, not just on hard load.
- [ ] **Scalar panel:** Often missing `turbo:load` re-init — verify it works after clicking between scenarios, not only on refresh.
- [ ] **Login/session:** Often missing 401→top-level redirect — verify expired session redirects cleanly (no frame error).
- [ ] **Login/session:** Often missing logout + revocation handling — verify logout clears session and disabled account is locked out.
- [ ] **List/search:** Often missing server-side paging — verify paging hits the API, not in-memory slicing.
- [ ] **List/search:** Often missing debounce — verify typing doesn't fire a request per keystroke.
- [ ] **Detail page:** Often missing parallel detail+relations fetch — verify it isn't two serial round-trips.
- [ ] **CDN assets:** Often missing version pins — verify Scalar/Turbo/Bootstrap are pinned, not `@latest`.
- [ ] **HttpClient:** Often missing `IHttpClientFactory` — verify no `new HttpClient()` per request.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Forms return 200 (Turbo swallows) | LOW | Add 303-on-success / 422-on-invalid base helper; retrofit handlers |
| Scalar breaks on Turbo nav | LOW–MEDIUM | Move init to `turbo:load`; add teardown; pin version |
| Credentials in session with no expiry/revocation | MEDIUM | Add absolute timeout, `isauthenticated` revalidation, logout flow |
| HTTP N+1 on list | MEDIUM | Refactor to render from `find` only; move enrichment to detail page |
| Antiforgery 400s under Turbo | LOW | Ensure tag-helper forms; add meta-token for custom fetches |
| Socket exhaustion from `new HttpClient` | MEDIUM | Switch to typed `IHttpClientFactory` client with auth handler |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| 200-response breaks Turbo forms | Role edit phase | Invalid submit shows errors; valid submit navigates |
| Scalar breaks on Turbo nav | Scalar side-panel phase | Panel works after navigating between scenarios |
| Credentials/session without expiry+revocation | Auth/session-proxy foundation | Revoked-account + idle-timeout tests pass |
| 401 redirect fights Turbo Frames | Auth/session-proxy foundation | Expired session redirects to login cleanly |
| HTTP N+1 + offset pagination | Roles list/search/pagination | Paging hits API; one round-trip per row-set |
| Antiforgery under Turbo | Role edit phase | Save works after Turbo nav, not just hard load |
| HttpClient lifecycle / Basic Auth handler | Auth/session-proxy foundation | Single typed client; auth header never logged |
| Bootstrap JS dead after Turbo nav | Layout/shell phase | Dropdowns/modals work after navigation |

## Sources

- Turbo Handbook — Navigate with Turbo Drive (303 redirect / 422 validation rule): https://turbo.hotwired.dev/handbook/drive (HIGH)
- Ben Nadel — Turbo Drive requires non-2xx for failed form submissions: https://www.bennadel.com/blog/4385-hotwire-turbo-drive-requires-failed-form-submissions-to-return-a-non-2xx-status-code.htm (MEDIUM)
- hotwired/turbo Issue #84 — redirect status code (303) clarification: https://github.com/hotwired/turbo/issues/84 (MEDIUM)
- Handling missing frames in Turbo (Coorasse): https://coorasse.com/blog/handling-missing-frames-in-turbo/ (MEDIUM)
- hotwired/turbo Issue #432 / #670 — no matching `<turbo-frame>` / `turbo:frame-missing` behavior: https://github.com/hotwired/turbo/issues/432 (MEDIUM)
- Scalar API Reference — Getting Started / Configuration / CDN: https://scalar.com/products/api-references/getting-started , https://scalar.com/products/api-references/configuration (HIGH)
- Scalar CDN (cdnjs, version pinning): https://cdnjs.com/libraries/scalar-api-reference (HIGH)
- Microsoft Learn — Cookie authentication & ValidatePrincipal revocation: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie (HIGH)
- brokul.dev — Authentication cookie lifetime & sliding expiration (absolute-expiry guidance): https://brokul.dev/authentication-cookie-lifetime-and-sliding-expiration (MEDIUM)
- Microsoft Learn — ASP.NET Core session timeout / IdleTimeout: https://learn.microsoft.com/en-us/answers/questions/827217/ (HIGH)
- Project context: Backlot.Studio PROJECT.md and CLAUDE.md (HIGH)

---
*Pitfalls research for: .NET Razor Pages + TurboJS admin frontend with Basic Auth proxy and Scalar*
*Researched: 2026-06-21*
