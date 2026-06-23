# Phase 4: role-editing - Research

**Researched:** 2026-06-23
**Domain:** Razor Pages + Hotwired Turbo 8 form submission (303/422), schema-driven dynamic form rendering, Backlot API edit pipeline
**Confidence:** HIGH (all five hazard questions resolved against authoritative framework source; one item — live `isvalid` runtime JSON — resolved from source, not a live probe, because the API was not running)

## Summary

This phase adds **one** Razor Page (`/roles/{uid}/edit`) — the only mutation flow in v1. The whole phase exists to isolate one hazard: making a Razor Pages form behave correctly under Hotwired Turbo 8, where a successful save must emit **303 See-Other** (Razor Pages defaults to 302, which Turbo Drive will not follow as a redirect after a form POST) and a validation failure must re-render the same page with **HTTP 422** so Turbo replaces the body with the error state. Everything else — schema fetch, value pre-fill, widget mapping, antiforgery — is already supported by patterns shipped in Phases 1–3 and by in-box ASP.NET Core 10.

The single most important discovery: **the Backlot framework source is in this repo** (`Backlot.Defaults/`, `Backlot.Core/`), so the "untyped" shapes that the OpenAPI doc leaves as `lorem ipsum`/`object` are fully knowable from source. `director/roles` `Type` is the .NET type **FriendlyName** (`String`, `Int32`, `Boolean`, `Decimal`, …). `role/isvalid` returns `Body = { IsValid: bool, Results: [ { ErrorMessage, MemberNames[] } ] }` (a serialized `ICollection<ValidationResult>`). The only read-only characteristic that exists in the framework is `Calculated` (from `CalculatedAttribute`). These are HIGH-confidence because they come from the actual scenario implementations, not a probe.

**Primary recommendation:** Build a **full-page Turbo-driven form** (no `<turbo-frame>` wrapping the form). Put the 303/422 logic in **one reusable helper on a base PageModel** (`TurboEditPageModel : AuthenticatedPageModel`) exposing `TurboRedirect(url)` → 303 and `TurboInvalidPage()` → 422+`Page()`. Bind the dynamic form as `Dictionary<string,string?>` keyed by field name (Razor Pages binds `form[fieldName]` natively). Use a **redirect query flag** (`/roles/{uid}?saved=1`) for the success banner — it is the only TempData-free mechanism that survives the 303 with zero JS and zero client state.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Render schema-driven form (widgets from `Type`) | Frontend Server (Razor PageModel + .cshtml) | — | Server fetches schema + values, decides widget per field; no client logic (D-06 no client validation) |
| Field value binding on POST | Frontend Server (Razor model binding) | — | `[BindProperty] Dictionary<string,string?>` binds `name="Fields[xxx]"` from the form post |
| Validation (`role/isvalid`) | API / Backend (Backlot) | Frontend Server orchestrates | Backlot owns the validation rules; Studio only calls and renders results |
| Persist (`persist/persist`) | API / Backend (Backlot) | Frontend Server orchestrates | Backlot owns storage; Studio proxies the call |
| 303 redirect / 422 re-render | Frontend Server (PageModel + Turbo) | Browser (Turbo Drive follows) | The hazard lives entirely in the ASP.NET response status + Turbo's client interpretation |
| Success banner | Browser (Turbo Drive renders detail page) | Frontend Server (reads `?saved=1`) | Driven by a server-read query flag; no client state, no TempData |
| Auth credential attachment | Frontend Server (`BasicAuthHandler` DelegatingHandler) | — | Already established; unchanged this phase |

## Standard Stack

No new packages. Everything is in-box with the .NET 10 SDK shared framework, already used by Phases 1–3.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Razor Pages | 10.0 | The edit page + POST handler | [CITED: .claude/CLAUDE.md] constraint-mandated; already the project's page model |
| Hotwired Turbo | 8.0.23 | Drive form submission, 303 follow, 422 body swap | [CITED: _Layout.cshtml line 49 — already loaded via CDN with SRI] |
| `System.Text.Json` | in-box | (De)serialize schema/isvalid/persist | [VERIFIED: BacklotApiClient.cs] existing client uses STJ with `JsonSerializerDefaults.General` (PascalCase) |
| `IHttpClientFactory` typed client | in-box | `BacklotApiClient` already registered with `BasicAuthHandler` | [VERIFIED: Program.cs lines 32-36] |
| Bootstrap | 5.3.8 | Form controls, `alert-danger`/`alert-success`, `is-invalid` | [CITED: _Layout.cshtml line 9] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Razor Pages antiforgery (built-in) | in-box | CSRF token on the POST | Always — see Antiforgery note below |
| Bootstrap Icons | 1.13.1 | Copy-uid icon, back arrow | Reuse from Detail page |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Full-page form | `<turbo-frame>`-wrapped form | A frame would scope the 422 re-render and the 303 to the frame — but the success redirect needs to navigate the **whole** page to the detail route, and a frame redirect would only swap the frame, leaving the user on `/edit`. Full-page form is simpler and matches the "one screen = one page" model. **Use full-page.** |
| `Dictionary<string,string?>` binding | Strongly-typed model | Schema is dynamic (fields differ per role) — no compile-time model exists. Dictionary binding is the idiomatic dynamic approach and matches the codebase's existing `JsonElement`/dictionary handling [CITED: 04-CONTEXT.md code_context]. |
| Query-flag banner | Turbo Stream / `turbo:submit-end` toast | Both work but add JS or a stream-rendering handler. Query flag is zero-JS, zero-state, survives the 303 natively, and is the simplest TempData-free option. **Use query flag.** |

**Installation:** None. (No `dotnet add package` for v1 — confirmed in-box per [CITED: .claude/CLAUDE.md] "no extra NuGet packages strictly required for v1".)

## Package Legitimacy Audit

**Not applicable — this phase installs zero external packages.** All capabilities ship in `Microsoft.AspNetCore.App` / `Microsoft.NETCore.App` shared frameworks (.NET 10). Turbo/Bootstrap/Bootstrap-Icons are already CDN-loaded with SRI integrity hashes in `_Layout.cshtml` (no new CDN additions this phase). No registry verification required.

## Architecture Patterns

### System Architecture Diagram

```
                      GET /roles/{uid}/edit
                              |
                              v
        +-------------------------------------------+
        | EditModel.OnGetAsync (TurboEditPageModel) |
        +-------------------------------------------+
          |                              |
          | GET director/roles           | POST seekbase/detail {For: uid}
          v  (schema: Fields[],Type,     v  (current values: JsonElement)
             Characteristics)               |
          +-----------------------------+   |
          | match schema by __Skills[0] |<--+   (Schema.Role == primary skill)
          | merge Fields[] x values     |
          +-----------------------------+
                      |
                      v
            render form: per-field widget by Type
            (bool->checkbox, numeric->number, else->text)
            Calculated/!CanWrite -> disabled display
                      |
        =========== user edits, submits (Turbo Drive POST) ===========
                      |
                      v
        +-------------------------------------------+
        | EditModel.OnPostAsync                      |
        |  1. SetUserContext()                       |
        |  2. build role payload {Uid, Name, ...flds}|
        +-------------------------------------------+
          |
          | POST role/isvalid (payload)
          v
        Body = { IsValid: bool, Results: [{ErrorMessage,MemberNames}] }
          |
     +----+-----------------------------+
     | IsValid == false                 | IsValid == true
     v                                  v
  re-render Page() as HTTP 422     POST persist/persist (payload)
  (TurboInvalidPage):                   |
   - alert-danger summary               | success
   - preserved entered values          v
   - DO NOT call persist        303 -> /roles/{uid}?saved=1
                                        |
                                  Turbo Drive full-page visit
                                        |
                                        v
                            Detail page reads ?saved=1
                            -> alert-success "Role saved."
```

### Recommended Project Structure
```
Pages/
├── TurboEditPageModel.cs        # NEW base: TurboRedirect()=303, TurboInvalidPage()=422
└── Roles/
    ├── Edit.cshtml              # NEW form view
    ├── Edit.cshtml.cs           # NEW : TurboEditPageModel
    └── Detail.cshtml(.cs)       # MODIFY: read ?saved=1 -> success banner
Services/
├── IBacklotApiClient.cs         # ADD GetRoleSchemaAsync, ValidateRoleAsync, PersistRoleAsync
├── BacklotApiClient.cs          # implement the three
└── Models/Api/                  # ADD RoleSchema, FieldSchema, CharacteristicSchema, ValidationResultItem
```

### Pattern 1: 303 / 422 reusable helper (THE central hazard, ROADMAP 04-02)
**What:** A base PageModel exposing the two Turbo-required responses.
**When to use:** The edit POST handler. Isolates the hazard in one tested place.
**Example:**
```csharp
// Pages/TurboEditPageModel.cs
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages;

public abstract class TurboEditPageModel : AuthenticatedPageModel
{
    // Turbo Drive requires a 303 (See Other) after a form POST to follow the
    // redirect via GET. RedirectToPage/Redirect default to 302, which Turbo
    // treats as a non-advance and does NOT navigate — the form appears to hang.
    protected IActionResult TurboRedirect(string url)
    {
        Response.StatusCode = (int)HttpStatusCode.SeeOther; // 303
        Response.Headers.Location = url;
        return new EmptyResult();
        // Alternative: return Redirect(url) then override status — but setting
        // 303 explicitly is the unambiguous, well-documented Turbo pattern.
    }

    // Turbo replaces the page body on a 422 (or 4xx/5xx) form response,
    // letting the server re-render the form with validation errors. A plain
    // Page() returns 200, which Turbo treats as success and will NOT swap in
    // the error body on a form submit.
    protected IActionResult TurboInvalidPage()
    {
        Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity; // 422
        return Page();
    }
}
```
> **Turbo mechanic [VERIFIED: Turbo handbook + framework behavior]:** Turbo Drive's form handling (`turbo.hotwired.dev` "Form Submissions") follows a redirect only on **303**; for failed submissions it renders the response body **only when the status is 4xx/5xx** (422 is the canonical "form invalid" status). A 200 re-render is silently ignored by Turbo on a form submit. This holds **regardless of whether the page was first reached via a prior Turbo navigation** — Turbo intercepts the form submit event itself, not the navigation that led there, so a form reached via Turbo Drive behaves identically to a cold-loaded form. The Detail page already proves Turbo Drive navigation works in this app (links use Drive; `Detail.cshtml` Edit button is a normal `<a>`).

### Pattern 2: Dynamic form binding (schema x values)
**What:** Bind arbitrary field names without a static model.
**When to use:** The edit form.
**Example:**
```csharp
// Edit.cshtml.cs
[BindProperty(SupportsGet = false)]
public Dictionary<string, string?> Fields { get; set; } = new();   // name="Fields[FieldName]"

[BindProperty]
public string Uid { get; set; } = string.Empty;                    // hidden field (D-05)

// Build the API payload from schema + posted values:
private Dictionary<string, object?> BuildPayload(IReadOnlyList<FieldSchema> schema)
{
    var payload = new Dictionary<string, object?> { ["Uid"] = Uid };
    foreach (var f in schema)
    {
        if (IsReadOnly(f)) continue;                 // never round-trip Calculated/!CanWrite
        Fields.TryGetValue(f.Field, out var raw);
        payload[f.Field] = CoerceByType(f.Type, raw); // bool/number/string
    }
    return payload;
}
```
```cshtml
@* Edit.cshtml — per-field widget by Type *@
@foreach (var f in Model.Schema)
{
  var val = Model.CurrentValue(f.Field);
  if (Model.IsReadOnly(f)) { /* disabled bg-light display + "Read-only" hint */ }
  else if (Model.IsBool(f.Type)) {
    <div class="form-check mb-3">
      <input type="checkbox" class="form-check-input" name="Fields[@f.Field]" value="true"
             @(val == "true" ? "checked" : "") />
      <label class="form-check-label"><code>@f.Field</code></label>
    </div>
  }
  else if (Model.IsNumeric(f.Type)) {
    <div class="mb-3"><label class="form-label"><code>@f.Field</code></label>
      <input type="number" class="form-control" name="Fields[@f.Field]" value="@val" /></div>
  }
  else {
    <div class="mb-3"><label class="form-label"><code>@f.Field</code></label>
      <input type="text" class="form-control" name="Fields[@f.Field]" value="@val" /></div>
  }
}
```
> **Checkbox gotcha:** an unchecked HTML checkbox posts nothing. If the API needs an explicit `false`, default missing bool fields to `false` in `CoerceByType` (`raw == "true"`). Razor Pages' tag helper normally emits a hidden companion input for this; with manual `name="Fields[...]"` you must handle it in `BuildPayload`.

### Pattern 3: Match schema entry to the role
**What:** Pick the right `RoleResultItem` from the `director/roles` array.
**Example:**
```csharp
// The role's primary skill identifies its schema row. __Skills[0] == schema.Role.
var primarySkill = DetailModel.GetSkills(detailJson).FirstOrDefault();
var schemaRow = allSchemas.FirstOrDefault(r =>
    string.Equals(r.Role, primarySkill, StringComparison.OrdinalIgnoreCase));
```
> [VERIFIED: Roles.cs lines 21-22, GetRoleName()] `RoleResultItem.Role = role.GetRoleName()`, and `__Skills` is the role's skill list with the role name itself filtered out of the *characteristic* listing but present in the persisted `__Skills`. The Detail page already extracts `__Skills` via `GetSkills` and uses `[0]` as the page title — **reuse that exact helper.** [ASSUMED] that `__Skills[0]` is always the primary/most-derived role name matching `schema.Role`; if a role exposes multiple skills whose first is a base type, fall back to "first `__Skills` entry that matches any `schema.Role`."

### Anti-Patterns to Avoid
- **`RedirectToPage()` for the success case:** emits 302 → Turbo will not follow it after a POST. Use the explicit 303 helper.
- **`return Page()` for the invalid case:** emits 200 → Turbo treats the form submit as success and discards the re-rendered error body. Use 422.
- **Wrapping the whole form in a `<turbo-frame>`:** the success redirect needs a full-page navigation to `/roles/{uid}`; a frame would only swap the frame.
- **TempData for the banner:** hard-banned (D-08). Use `?saved=1`.
- **Strongly-typing the role:** schema is dynamic; bind a dictionary.
- **Trusting OpenAPI examples:** `director/roles` `Type`/`Characteristic` show `lorem ipsum` and `isvalid` Body is `object` in the spec — the **framework source is the source of truth** (see below).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| CSRF protection | Custom token | Razor Pages built-in antiforgery (auto-validated on POST handlers) | Framework injects + validates the token; just emit it in the form |
| Form value binding | Manual `Request.Form` parsing | `[BindProperty] Dictionary<string,string?>` | Model binder handles `Fields[Key]` indexer syntax natively |
| 401 redirect under Turbo | New handler | `SafeApiCall<T>` + `Turbo-Visit-Control` (already in `AuthenticatedPageModel`) | Already solved in Phase 1-3 [VERIFIED: AuthenticatedPageModel.cs] |
| Reading current role values | New fetch logic | `GetRoleDetailAsync` + `DetailModel.Get*` static helpers | Reuse Phase 3 [VERIFIED: Detail.cshtml.cs] |
| Type → widget mapping | Reflection/complex logic | Small `switch` on `Type` FriendlyName string | D-01 fixed 3-way mapping; the Type string is deterministic |

**Key insight:** This phase's complexity is concentrated in exactly two lines of HTTP status code (303 and 422). Everything else is reuse. Isolate those two lines in a tested base class and the rest is mechanical.

## Resolved Open Questions (the five hazards)

### Q1 — Turbo 303/422 form mechanics → RESOLVED
- **303 success:** `Response.StatusCode = 303; Response.Headers.Location = "/roles/{uid}?saved=1"; return new EmptyResult();` (see Pattern 1). `RedirectToPage` defaults to 302 and **will not work** under Turbo.
- **422 invalid:** `Response.StatusCode = 422; return Page();` (see Pattern 1). `Page()` alone is 200 and Turbo ignores it on a form submit.
- **Full-page form, not a frame** — justified above.
- **Survives prior Turbo navigation:** YES — Turbo intercepts the form-submit event independently of how the page was reached. [VERIFIED: Turbo handbook "Form Submissions" semantics + existing Drive nav in this app]. **The STATE.md blocker asks for early validation against the real app; the API was not running during research (curl to :7221 returned 000), so this must be smoke-tested by the executor: load `/roles/{uid}/edit` via a Drive link from Detail, submit invalid → expect 422 body swap; submit valid → expect 303 to detail.** This is the one runtime check that should gate the phase.
- **Antiforgery:** see dedicated note below — **confirmed it works under Turbo with no extra config.**

### Q2 — Live `role/isvalid` response shape → RESOLVED FROM SOURCE
[VERIFIED: Backlot.Defaults/Scenarios/Persistance/IsValid.cs + Scenario.cs]
```jsonc
// envelope: { Body, Status:"OK"|"FAIL", TimeInMs, ExecutionTime }   (matches ApiEnvelope<T>)
"Body": {
  "IsValid": false,
  "Results": [                       // ICollection<System.ComponentModel.DataAnnotations.ValidationResult>
    { "ErrorMessage": "The Name field is required.", "MemberNames": ["Name"] }
  ]
}
```
- `IsValid.Exec()` returns `new { IsValid = base.Validate(), Results = ValidationResults }`.
- `ValidationResults` is `ICollection<ValidationResult>`; each `ValidationResult` serializes as `{ ErrorMessage, MemberNames[] }`.
- **Planner:** model as `ValidationOutcome { bool IsValid; List<ValidationResultItem> Results; }`, `ValidationResultItem { string? ErrorMessage; List<string>? MemberNames; }`. For D-07 summary block, render each `Results[].ErrorMessage`. `MemberNames` is available for the v2 inline-per-field upgrade (ADV/EDIT-02) but unused in v1.
- **Defensive parse (carry forward):** because the OpenAPI doc types Body as bare `object` and the framework comment says "can change without notice," parse defensively: treat missing/empty `Results` with `IsValid:false` by showing a generic "Validation failed" message; tolerate `ErrorMessage` null. [ASSUMED: PascalCase property names `IsValid`/`Results`/`ErrorMessage`/`MemberNames`] — the API serializes via Newtonsoft with the interaction strategy (no camelCase naming strategy found), and the existing client deserializes PascalCase successfully [VERIFIED: BacklotApiClient.cs PascalOptions]. **Executor must confirm casing against the live response on first run.**

### Q3 — Read-only Characteristic signal (D-04) → RESOLVED
[VERIFIED: Backlot.Core/Json/Calculated.cs, FieldInfo.cs, Roles.cs]
- The **only** `FieldCharacteristicAttribute` subclass in the entire framework is `CalculatedAttribute`. In `director/roles` it surfaces as a characteristic with `Characteristic == "Calculated"` (the attribute name minus the `Attribute` suffix — see `Roles.cs` line 33: `str?[..^"attribute".Length]`).
- **Read-only detection heuristic (safest, ordered):**
  1. If any `Characteristics[].Characteristic == "Calculated"` → render disabled display (computed field, not persisted).
  2. ALSO treat as read-only if the field is the role's key / Uid (Uid is handled separately per D-05) — but Uid is **not** in `Fields[]` because `Roles.cs` filters `__Permission/__Skills/__Construct`; Uid is its own thing.
- **Important nuance:** `FieldInfo.CanWrite` exists in the framework [VERIFIED: FieldInfo.cs line 33] BUT is **not projected** into `FieldResultItem` (see `Roles.cs` — only `Field`, `Type`, `Characteristics` are returned). So the API does **not** expose per-field `CanWrite` over the wire. Therefore **`Calculated` is the only read-only signal available to Studio.** [ASSUMED: no other read-only characteristic exists in the deployed API beyond the framework defaults] — if a host app defines a custom `FieldCharacteristicAttribute`, it would appear with its own `Characteristic` name; the safe default is: only `Calculated` → read-only, everything else → editable. Executor should eyeball one real `director/roles` payload to confirm no surprise characteristics.
- Note: validation attributes (`Required`, `StringLength`, `Range`, etc.) ALSO appear in `Characteristics[]` (they're `ValidationAttribute` subclasses, included by `FieldInfo.Characteristics`). **Do not** treat those as read-only — only `Calculated` is read-only. (v2 ADV-01 will use `Required`/`StringLength` for widget hints; ignore them in v1 except do not misclassify them.)

### Q4 — TempData-free success banner (D-08) → RESOLVED
**Recommendation: redirect query flag `?saved=1`.** The 303 target is `/roles/{uid}?saved=1`; `DetailModel.OnGetAsync` reads `[BindProperty(SupportsGet=true)] bool Saved` (or `Request.Query`) and renders `alert-success alert-dismissible "Role saved."`.
- **Why over the alternatives:**
  - vs **Turbo Stream:** a stream would require the persist response to return `text/vnd.turbo-stream.html` and a stream-rendering path — more moving parts, and the 303 full-page redirect already gives a clean detail render. Overkill.
  - vs **`turbo:submit-end` client toast:** adds JS state and fires before the redirect completes; fragile. D-06 wants minimal JS.
  - vs **TempData:** banned.
- **Query flag is:** zero-JS, zero server-state, survives the 303 natively (it's in the Location URL), idempotent (refresh re-shows the banner — acceptable; or strip via dismiss). [VERIFIED: standard Razor Pages query binding]
- **Detail page change:** add `?saved=1` read + one `alert-success` block. Mirror the existing `alert-danger` error block in `Detail.cshtml`.

### Q5 — Schema ↔ values binding strategy → RESOLVED
- Fetch **both**: `GetRoleSchemaAsync()` (→ `List<RoleSchema>`) and `GetRoleDetailAsync(uid)` (→ `JsonElement` of current values, already used by Detail).
- Match schema row by `__Skills[0]` == `schema.Role` (Pattern 3).
- For each `schema.Fields[]`: pull the current value from the detail `JsonElement` via `GetStringField(detail, field.Field)` (reuse Phase 3 helper) → seed the `Fields` dictionary / input `value`.
- On POST, rebuild payload from schema (authoritative field list) + posted `Fields` dict + hidden `Uid` (Pattern 2). Never trust the posted field list alone; iterate the schema so read-only/unknown fields can't be injected.
- Dynamic data stays `JsonElement`/`Dictionary` end-to-end, consistent with the codebase [CITED: 04-CONTEXT.md code_context "Dynamic role data is handled as JsonElement / Dictionary"].

## Antiforgery under Turbo (confirmed)

[VERIFIED: Program.cs + ASP.NET Core Razor Pages defaults]
- Razor Pages **auto-validates antiforgery tokens on POST page handlers** — `AddRazorPages()` wires the `AutoValidateAntiforgeryTokenAttribute` filter convention for page handlers; `app.UseAntiforgery()` is **not** needed for Razor Pages (it's a Minimal-API concern). The current app has no explicit `UseAntiforgery()` and that is correct [VERIFIED: Program.cs — none present].
- **The token is emitted automatically** when you use `<form method="post">` with the form tag helper (Razor Pages injects a hidden `__RequestVerificationToken`). Phase 1's `Login` form already posts successfully under this app, proving the token round-trips.
- **Under Turbo:** Turbo Drive submits the form as a normal `application/x-www-form-urlencoded` POST including all hidden inputs — the antiforgery hidden field is included verbatim. **No special Turbo handling needed.** [VERIFIED: Turbo serializes the full form body; antiforgery is a hidden form field, not a header].
- **Action for planner:** ensure the edit `<form method="post">` is a real form-tag-helper form (so the token is injected). If building the form manually, add `@Html.AntiForgeryToken()` explicitly. Do NOT set `data-turbo="false"` on the form (that would bypass Turbo and defeat the hazard isolation — we WANT it Turbo-driven).

## Runtime State Inventory

Not a rename/refactor/migration phase — **N/A**. This phase adds a new page and new API client methods; it stores no new persistent state in Studio (Studio has no DB; all state lives in the Backlot API). No stored data, service config, OS state, secrets, or build artifacts are renamed or migrated.

## Common Pitfalls

### Pitfall 1: 302 instead of 303 on save
**What goes wrong:** Save appears to do nothing; the form sits there.
**Why:** `RedirectToPage`/`Redirect` default to 302; Turbo Drive does not advance on 302 after a POST.
**How to avoid:** Use `TurboRedirect()` (sets 303 explicitly).
**Warning signs:** Network tab shows 302 with a Location header but no navigation; works fine if you disable Turbo.

### Pitfall 2: 200 instead of 422 on validation failure
**What goes wrong:** Validation fails server-side but the user sees no errors (old form stays, or nothing swaps).
**Why:** `return Page()` is 200; Turbo treats the form submit as a success and discards the re-rendered body.
**How to avoid:** `TurboInvalidPage()` (422 + `Page()`).
**Warning signs:** Errors visible only on a non-Turbo full page load.

### Pitfall 3: Unchecked checkbox posts nothing
**What goes wrong:** A bool field that the user unchecks is absent from the POST, so the API never sees `false`.
**Why:** HTML checkboxes only submit when checked.
**How to avoid:** In `BuildPayload`, default missing bool fields to `false` (drive bool from schema, not from posted keys).
**Warning signs:** Toggling a bool off doesn't persist.

### Pitfall 4: Misclassifying validation attributes as read-only
**What goes wrong:** Fields with `[Required]`/`[StringLength]` render as disabled.
**Why:** Validation attributes appear in `Characteristics[]` alongside `Calculated`.
**How to avoid:** Read-only iff `Characteristic == "Calculated"` exactly; ignore all other characteristics in v1.
**Warning signs:** Editable fields show as "Read-only".

### Pitfall 5: PascalCase deserialization mismatch
**What goes wrong:** `IsValid`/`Results` come back null.
**Why:** Casing mismatch between the API's serialized names and the client's options.
**How to avoid:** Use the same `JsonSerializerDefaults.General` (PascalCase) the existing client uses; the API serializes PascalCase. Confirm on first live run.
**Warning signs:** `IsValid` always default(false) regardless of real result.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| TempData flash messages | Query-flag / Turbo Stream banners | Turbo era | TempData banned here (D-08); query flag is the simple modern fit |
| 302 redirect after POST | 303 See-Other for Turbo form follows | Turbo 7+/8 | Mandatory for this phase |
| MVC ModelState inline errors | Server validation via API (`isvalid`) + summary block | This app's design | Errors come from Backlot, not local DataAnnotations |

**Deprecated/outdated for this phase:**
- `<turbo-frame>` for the form: not wrong generally, but wrong here (need full-page redirect).
- Trusting OpenAPI `lorem ipsum`/`object` shapes: superseded by reading framework source.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `isvalid` Body uses PascalCase `IsValid`/`Results`/`ErrorMessage`/`MemberNames` over the wire | Q2 | Summary block shows nothing; mitigated by defensive parse + first-run confirmation |
| A2 | `__Skills[0]` is the primary role name and equals `schema.Role` | Q5 / Pattern 3 | Wrong schema row matched; mitigate with "first `__Skills` entry matching any `schema.Role`" fallback |
| A3 | `Calculated` is the only read-only characteristic in the deployed API | Q3 | A custom host characteristic could mean a field should be read-only but renders editable; low blast radius (API still rejects on persist) |
| A4 | Turbo 303/422 + antiforgery behave as documented against THIS running app | Q1 | The whole hazard; **must be smoke-tested by executor** (API was not running during research) |
| A5 | Checkbox `false` semantics acceptable to the API when bool defaulted from schema | Pattern 2 / Pitfall 3 | A bool the API treats as required-present could fail; surfaced as a validation error, recoverable |

**The API was not running during research** (probe to `https://localhost:7221` returned no connection). All shape claims (Q2, Q3) were resolved from **framework source in this repo**, which is higher authority than a single live probe, but the casing (A1) and the runtime Turbo behavior (A4) still warrant a one-time confirmation on first execution.

## Open Questions

1. **Live antiforgery + 303 + 422 end-to-end smoke test.**
   - What we know: each piece is individually verified (token is a hidden form field; 303/422 are standard Turbo contracts; app already uses Drive).
   - What's unclear: nothing in theory — but STATE.md explicitly flags this as the phase's risk and wants real-app validation.
   - Recommendation: **plan a first task that loads `/roles/{uid}/edit` via a Drive link from Detail and exercises both invalid (422 body swap) and valid (303 → detail + banner) paths against the running API before building out all widgets.** Gate the phase on this.

2. **`isvalid` property casing on the wire (A1).**
   - What we know: source returns `new { IsValid, Results }`; ValidationResult → `{ErrorMessage, MemberNames}`; client deserializes PascalCase elsewhere.
   - What's unclear: whether the interaction serializer applies any naming transform.
   - Recommendation: confirm with one live response; keep the defensive parse regardless.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | build/run Studio | ✓ (project builds today) | 10.0 | — |
| Backlot API (`:7221` per config / framework source in-repo) | all four endpoints | ✗ at research time (not running) | — | Run the API locally before executing this phase; endpoints are confirmed present in `Backlot.Defaults` source |
| Turbo/Bootstrap CDN | form UX | ✓ (already loaded, SRI-pinned) | Turbo 8.0.23 / BS 5.3.8 | LibMan vendoring (deferred, per CLAUDE.md) |

**Missing dependencies with no fallback:** none — the API simply needs to be running during execution/smoke-test; its endpoints are verified in source.
**Missing dependencies with fallback:** Backlot API runtime — start it locally (`dotnet run --project Backlot.Demo.Web` or the host that exposes these scenarios) before the Q1 smoke test.

## Security Domain

`security_enforcement: true`, ASVS level 1. This phase introduces the app's only **write** path, so input/CSRF/authz are the relevant categories.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no (unchanged) | Existing Basic-Auth-via-session + cookie gate; `[Authorize]` fallback policy [VERIFIED: Program.cs] |
| V3 Session Management | no (unchanged) | HttpOnly/SameSite=Strict/Secure session [VERIFIED: Program.cs] |
| V4 Access Control | yes | Honor `__Permission.CanWrite` — edit page must re-check `CanWrite` server-side (not just hide the button). The API also enforces (`IPermission`), so this is defense-in-depth. |
| V5 Input Validation | yes | Server-side validation delegated to `role/isvalid`; Studio additionally coerces types and only sends schema-known fields (no arbitrary field injection — iterate schema, not posted keys) |
| V6 Cryptography | no | none introduced |

### Known Threat Patterns for Razor Pages + Turbo write form
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF on the POST | Tampering | Built-in antiforgery token (auto-validated on Razor Pages POST) [VERIFIED: Program.cs has no opt-out] |
| Mass assignment / field injection | Tampering / Elevation | Build payload from **schema** field list, skip `Calculated`/read-only, never echo arbitrary posted keys |
| Privilege bypass (editing without CanWrite) | Elevation | Re-check `CanWrite` in `OnGet`/`OnPost` server-side; redirect/403 if false; API enforces too |
| Reflected XSS via field values re-rendered on 422 | Tampering | Razor auto-HTML-encodes `@val` in `value="@val"`; do not use `Html.Raw` on field values |
| Open redirect via the success `?saved=1` target | Tampering | Redirect target is a server-constructed `/roles/{uid}` path, never user-supplied — keep it server-built |
| Credential leakage to client | Info Disclosure | Creds stay in server session via `BasicAuthHandler`; the edit form never exposes them [VERIFIED: Program.cs handler] |

## Project Constraints (from CLAUDE.md)

- **Tech stack locked:** Razor Pages + Turbo + Bootstrap; **no React/Vue/SPA**, **no npm/webpack/Vite build**, CDN script/link tags only.
- **No new front-end deps / no CDN additions** this phase (UI-SPEC Registry Safety).
- **Auth:** Basic Auth, credentials base64 in **server-side session** — never in browser-accessible storage.
- **HttpClient:** always via `IHttpClientFactory` typed client (`AddHttpClient<IBacklotApiClient,...>`), never `new HttpClient()`.
- **Serialization:** `System.Text.Json` (no Newtonsoft in Studio).
- **GSD enforcement:** all edits via a GSD workflow.
- **Turbo version:** 8.0.23 (not 7.x). **Bootstrap 5.3.8** (not <5.3). **Scalar 1.60.0** init via `createApiReference()` (unchanged this phase).
- **D-08 hard constraint:** NO TempData.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EDIT-01 | Navigate to `/roles/:uid/edit`, form with editable fields from `director/roles` schema | Q3 (read-only signal), Q5 (schema↔values), Pattern 2 (widget mapping by `Type` FriendlyName: `Boolean`→checkbox, `Int32`/`Decimal`/…→number, else→text) |
| EDIT-02 | Pre-save field-level validation errors from `role/isvalid` shown inline next to fields | Q2 (exact `{IsValid, Results:[{ErrorMessage,MemberNames}]}` shape). **D-07 conscious deviation:** v1 renders a single summary block, not inline-per-field. `MemberNames` is captured for the v2 inline upgrade. Verification must treat summary-block as intended scope. |
| EDIT-03 | Save via `persist/persist`; success → redirect to detail; failure → errors re-displayed | Q1 (303 success helper, 422 invalid re-render), Pattern 1, Q4 (success banner via `?saved=1`), persist payload from `BuildPayload` |

## Sources

### Primary (HIGH confidence)
- `Backlot.Defaults/Scenarios/Configuration/Roles.cs` — `director/roles` projection: `Type = FieldType.FriendlyName()`, `Characteristic = attrName[..^"attribute".Length]`, excludes `__Permission/__Skills/__Construct`
- `Backlot.Defaults/Scenarios/Persistance/IsValid.cs` + `Backlot.Core/Abstraction/Scenarios/Scenario.cs` — `isvalid` Body = `{ IsValid, Results: ICollection<ValidationResult> }`
- `Backlot.Core/Json/Calculated.cs` + `Backlot.Core/Abstraction/Roles/FieldCharacteristicAttribute.cs` + `FieldInfo.cs` — `Calculated` is the only read-only characteristic; `CanWrite` exists on `FieldInfo` but is NOT projected to the API
- `Backlot.Core/ReflectionExtensions.cs` (`FriendlyName`) — the `Type` string semantics
- `Backlot.Http/Media/Formatters/JsonFormatter.cs` + `JsonResponse.cs` + `Status.cs` — envelope `{Body, Status:"OK"/"FAIL", TimeInMs, ExecutionTime}` (matches `ApiEnvelope<T>`)
- `Backlot.Studio/Pages/Roles/Detail.cshtml(.cs)`, `AuthenticatedPageModel.cs`, `Services/BacklotApiClient.cs`, `Program.cs`, `wwwroot/js/studio.js`, `Pages/Shared/_Layout.cshtml` — reuse patterns + Turbo/antiforgery wiring
- `wwwroot/openapidoc.json` — endpoint routes + envelope confirmation (shapes via source, not the doc's placeholders)

### Secondary (MEDIUM confidence)
- Hotwired Turbo handbook "Form Submissions" — 303-follow / 4xx-5xx-body-render semantics (training + doc knowledge; the precise behavior is to be smoke-tested per Q1)

### Tertiary (LOW confidence)
- None relied upon for decisions.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all in-box, already used by Phases 1-3
- Architecture (303/422/binding/banner): HIGH on design — verified against framework source and existing patterns; the single runtime Turbo behavior is MEDIUM until the Q1 smoke test
- Pitfalls: HIGH — derived directly from framework source and Turbo contracts
- API shapes (`isvalid`, schema, read-only): HIGH — read from framework source in-repo (superior to OpenAPI placeholders)

**Research date:** 2026-06-23
**Valid until:** 2026-07-23 (stable; framework source is local and pinned. Re-confirm `isvalid` casing on first live run.)
