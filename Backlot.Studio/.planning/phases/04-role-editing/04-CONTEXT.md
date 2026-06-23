# Phase 4: role-editing - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

One new Razor Page (`/roles/{uid}/edit`) delivering the only mutation flow in v1. The page renders a schema-driven edit form built from the role's field schema (`GET /api/role/director/roles`), pre-filled from the existing `seekbase/detail` fetch. On save it runs `POST /api/role/role/isvalid` first; if invalid it re-renders the form (422) with the errors visible; if valid it calls `POST /api/role/persist/persist` and redirects (303) to the role detail page, where a "Role saved" confirmation appears. The Edit entry point and `CanWrite` gating already exist from Phase 3.

**Out of this phase:** relation editing, create-from-scratch, bulk edit, JSON-blob editing (all REQUIREMENTS.md "Out of Scope"); full schema-aware widgets and Characteristics-driven widget hints (v2 ADV-01); revision history (v2 HIST-01/02).

</domain>

<decisions>
## Implementation Decisions

### Field Widget Rendering
- **D-01:** "Minimal typed" rendering. Map schema field `Type` to a small set of controls: **bool → checkbox**, **numeric (int/decimal/etc.) → `<input type="number">`**, **everything else and any unrecognized Type → single-line text input**. No date pickers, dropdowns, or other typed widgets in v1 (those are v2 / ADV-01).
- **D-02:** **No textarea in v1.** All string fields are single-line text inputs regardless of length — multi-line detection is explicitly skipped for now.

### Editable Field Set
- **D-03:** **Schema fields only.** Render one input per field in the role's `director/roles` schema (`Fields[].Field`), pre-filled with the current value from the `seekbase/detail` fetch. `__`-prefixed system fields (`__Permission`, `__Skills`, `__LastModifiedDate`) are not part of the schema and are therefore naturally excluded from the form.
- **D-04:** **Honor a read-only characteristic.** If a field's `Characteristics` mark it as read-only / key / computed, render it as a disabled display value (shown for context, not editable) rather than an editable input. *Researcher must confirm the exact characteristic name/signal in the live `director/roles` response* — see Open Questions.
- **D-05:** **Uid handling.** The role's `Uid` is shown at the top of the form as read-only context (with the same copy-to-clipboard affordance as the detail page) AND carried in a hidden form field so it round-trips on save. It identifies the record and must never change.

### Validation Timing & Errors
- **D-06:** **Validate on submit, before persist.** A single save handler: call `role/isvalid` first; if invalid, re-render the form (422) with errors and **do not** call `persist`; if valid, call `persist/persist` then redirect (303) to detail. No client-side or on-blur live validation in v1.
- **D-07:** **Summary-block error display (conscious v1 deviation from EDIT-02).** Validation errors render in a single alert/summary block at the top of the form, **not** inline beside each field. EDIT-02's wording ("inline next to the relevant fields") is knowingly not fully met in v1; this is an accepted, recorded decision so verification treats it as intended scope, not a gap. Inline-per-field is a follow-up. This choice is robust to the still-unknown shape of the `isvalid` response `Body`.

### Save Confirmation
- **D-08:** After a successful save and 303 redirect, the detail page shows a "Role saved" success banner. **Hard constraint: NO TempData.** The mechanism must use a Turbo-native / Razor Pages-idiomatic approach (e.g., a redirect query flag the detail page reads, a Turbo Stream, or a `turbo:submit-end` client-side toast). Researcher to pick the cleanest idiomatic mechanism — but TempData is explicitly off the table.

### Claude's Discretion
- Turbo save mechanics (full-page form vs turbo-frame; how the 303-success and 422-invalid responses are produced and survive a prior Turbo navigation; antiforgery token handling). **The user deliberately left this area to research/planning** — it is the central hazard this phase isolates and the researcher must validate it against the real app early (per STATE.md blocker).
- C# model/binding strategy for the dynamic schema + values (e.g., how schema `Fields` merge with `seekbase/detail` values into the form model).
- Exact `RedirectToPage` → 303 mechanism (Razor Pages default is 302; Turbo form handling needs 303) and how to emit a 422 status while re-rendering the page.
- How the schema entry is matched to the current role (likely primary `__Skills[0]` ↔ schema `Role`).
- Handling of a non-validation persist failure (HTTP 500 / network): surface an error and preserve entered values (standard approach unless researcher finds a constraint).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Constraints & Architecture
- `.planning/PROJECT.md` — Core value, constraints (Razor Pages + Turbo + Bootstrap, no SPA), auth model
- `.planning/REQUIREMENTS.md` — EDIT-01, EDIT-02, EDIT-03 (the 3 requirements this phase satisfies); note the D-07 conscious deviation from EDIT-02 wording
- `.planning/ROADMAP.md` §Phase 4 — Goal, success criteria, plan breakdown (04-01 schema form, 04-02 validation + persist)

### Tech Stack (from CLAUDE.md)
- `.claude/CLAUDE.md` — Pinned versions (Bootstrap 5.3.8, Turbo 8.0.23, .NET 10), CDN patterns, auth handler pattern, "What NOT to Use" list

### API Endpoints (from openapidoc.json)
- `wwwroot/openapidoc.json` — OpenAPI spec; relevant endpoints for this phase:
  - `GET /api/role/director/roles` — response `{Body: [{Role, Fields:[{Field, Type, Characteristics:[{Characteristic, Parameters:[{Name, Value}]}]}], Skills}]}` — the field schema the form is built from
  - `POST /api/role/role/isvalid` — body: role data (`{Name, Uid, ...fields}`), response `{Body: object}` (**shape untyped — must be confirmed live**)
  - `POST /api/role/persist/persist` — body: role data, response `{Body: {Name, LastModified, __Permission, ...}}` (persisted role)
  - `POST /api/role/seekbase/detail` — body `{For: uid}`, response `{Body: object}` — reused to pre-fill current values

### UI Design Contract (to be created)
- `.planning/phases/04-role-editing/04-UI-SPEC.md` — phase has `UI hint: yes`; run `/gsd-ui-phase 04` to generate the visual/interaction contract for the edit form. Downstream agents MUST read it before implementing frontend.

### Existing Patterns to Follow
- `Pages/Roles/Detail.cshtml.cs` — closest analog: fetches `seekbase/detail` → `JsonElement`, has reusable static field-extraction helpers (`GetPermissions`, `GetSkills`, `GetNonSystemFields`, `GetStringField`, `GetPageTitle`) and `CanWrite` gating. The edit page reuses the same detail fetch for pre-fill.
- `Pages/Roles/Detail.cshtml` — the `Edit Role` button already links to `/roles/{uid}/edit`; the detail page is the 303 redirect target and the host for the D-08 success banner.
- `Services/IBacklotApiClient.cs` / `Services/BacklotApiClient.cs` — typed client to extend with `GetRoleSchemaAsync` (director/roles), `ValidateRoleAsync` (isvalid), `PersistRoleAsync` (persist). Existing `PostEnvelopeAsync` helper pattern from Phase 3.
- `Pages/AuthenticatedPageModel.cs` — `SetUserContext()` + `SafeApiCall<T>()` (401 → `/Login` redirect). The edit page model inherits this.
- `Program.cs` — antiforgery note: Razor Pages enables antiforgery validation on POST handlers by default; the Turbo form must carry the antiforgery token. No `app.UseAntiforgery()` is wired explicitly (Razor Pages does this via the framework) — researcher to confirm token flow under Turbo.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DetailModel` static helpers (`Pages/Roles/Detail.cshtml.cs`): `GetPermissions`, `GetSkills`, `GetNonSystemFields`, `GetStringField`, `GetPageTitle` — reusable for reading current role state into the edit form.
- `ApiEnvelope<T>` (`Services/ApiEnvelope.cs`) — wraps `{Body, Status, TimeInMs, ExecutionTime}`. Use for schema (`ApiEnvelope<List<RoleSchema>>` or similar), isvalid, and persist responses.
- `AuthenticatedPageModel.SafeApiCall<T>()` — wraps API calls, redirects to `/Login` on `BacklotApiUnauthorizedException`. The edit model uses this for every call.
- Copy-uid JS (`wwwroot/js/studio.js`, `[data-action="copy-uid"]` delegation) — reusable for the read-only Uid display (D-05).

### Established Patterns
- Page error handling: `catch (HttpRequestException or TaskCanceledException)` → set an `ErrorMessage` property → render `alert alert-danger`. Same pattern as Detail/Scenarios.
- Layout activation: `ViewData["ActiveNav"] = "roles"` → `_Sidebar.cshtml` highlights the Roles link.
- 401 handling under Turbo: non-frame pages set `Response.Headers["Turbo-Visit-Control"]` to force full navigation, avoiding 401 inside a Turbo Frame.
- Dynamic role data is handled as `JsonElement` / `Dictionary<string, JsonElement>` rather than strong types.

### Integration Points
- New page: `Pages/Roles/Edit.cshtml` + `Edit.cshtml.cs` with `@page "/roles/{uid}/edit"`, inheriting `AuthenticatedPageModel`.
- New client methods on `IBacklotApiClient` / `BacklotApiClient`: schema fetch, isvalid, persist.
- The save handler is where the 303/422 Turbo hazard concentrates — isolate it in a small, well-tested helper.

</code_context>

<specifics>
## Specific Ideas

- Widget mapping is fixed and small: bool → checkbox, numeric → number input, all else → single-line text. No textarea, no date/dropdown widgets in v1.
- Uid appears read-only at the top of the form (copy button, like the detail page) and also as a hidden field for round-tripping.
- Validation is a single on-submit path: isvalid → (invalid) 422 re-render with a top-of-form summary block / (valid) persist → 303 to detail.
- Success banner "Role saved" on the detail page after redirect — **must NOT use TempData**; use a Turbo/Razor-idiomatic mechanism.
- EDIT-02 inline-per-field error placement is consciously deferred; v1 uses a summary block.

</specifics>

<deferred>
## Deferred Ideas

- **Full typed widgets / schema-aware widget hints from `Characteristics`** (date pickers, dropdowns, etc.) — maps to v2 **ADV-01**. Out of v1 scope per D-01.
- **Inline-per-field validation errors** (the literal EDIT-02 phrasing) and **live/on-blur validation** — deferred follow-up; v1 ships summary-block + on-submit only (D-06/D-07).

None of the above were scope creep — all are explicit v1-vs-later boundary calls made during discussion.

</deferred>

---

*Phase: 4-role-editing*
*Context gathered: 2026-06-23*
