# Phase 4: role-editing - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 8 (5 new, 3 modified)
**Analogs found:** 8 / 8

All new/modified files have a strong in-repo analog. This phase is almost entirely reuse: the only genuinely novel surface is the two-line 303/422 Turbo status handling, which has no analog (no write/redirect path existed before) and must follow RESEARCH.md Pattern 1.

## File Classification

| New/Modified File | New/Mod | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|---------|------|-----------|----------------|---------------|
| `Pages/Roles/Edit.cshtml.cs` | new | page model | request-response (read+write) | `Pages/Roles/Detail.cshtml.cs` + `Pages/Login.cshtml.cs` | role-match (read from Detail, POST/bind from Login) |
| `Pages/Roles/Edit.cshtml` | new | view | form render | `Pages/Login.cshtml` (form+antiforgery) + `Pages/Roles/Detail.cshtml` (layout/uid/error) | role-match |
| `Pages/TurboEditPageModel.cs` | new | base page model | request-response (303/422) | `Pages/AuthenticatedPageModel.cs` | role-match (base class pattern) — **no analog for the 303/422 bodies** |
| `Services/IBacklotApiClient.cs` | modify | service contract | CRUD/request-response | existing methods in same file | exact |
| `Services/BacklotApiClient.cs` | modify | service impl | CRUD/request-response | `GetRoleDetailAsync`/`GetEnvelopeAsync`/`PostEnvelopeAsync` (same file) | exact |
| `Models/Api/RoleSchema.cs` (or add to existing) | new | DTO/model | transform | `Models/Api/RoleFind.cs`, `Models/Api/ScenarioItem.cs` | exact |
| `Models/Api/ValidationOutcome.cs` (or same file) | new | DTO/model | transform | `Models/Api/RoleFind.cs` (FindResult) | exact |
| `Pages/Roles/Detail.cshtml.cs` + `Detail.cshtml` | modify | page model + view | request-response | itself (add `?saved=1` read + `alert-success`) | exact (mirror existing `alert-danger`) |

## Pattern Assignments

### `Pages/TurboEditPageModel.cs` (base page model, 303/422)

**Analog:** `Pages/AuthenticatedPageModel.cs` — for the abstract-base-class shape and namespace (`Backlot.Studio.Pages`). The Edit model inherits this new base, which itself inherits `AuthenticatedPageModel`, so `SetUserContext()`/`SafeApiCall<T>()` remain available.

**Base-class pattern to copy** (`Pages/AuthenticatedPageModel.cs:1-32`):
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Backlot.Studio.Pages;

public abstract class AuthenticatedPageModel : PageModel
{
    protected void SetUserContext() { ViewData["Username"] = User.Identity?.Name ?? "Unknown user"; }

    protected async Task<(T? Value, IActionResult? Redirect)> SafeApiCall<T>(Func<Task<T>> apiCall)
    {
        try { return (await apiCall(), null); }
        catch (Services.BacklotApiUnauthorizedException)
        {
            Response.Headers["Turbo-Visit-Control"] = "reload";
            return (default, RedirectToPage("/Login"));
        }
    }
}
```

**New 303/422 bodies — NO ANALOG, use RESEARCH.md Pattern 1 verbatim** (RESEARCH.md lines 138-161). This is the central phase hazard. Note the existing `Turbo-Visit-Control` header manipulation in `SafeApiCall` (line 28) is the only prior art for writing Turbo-specific response state — follow that style (set `Response.StatusCode` / `Response.Headers` directly):
```csharp
public abstract class TurboEditPageModel : AuthenticatedPageModel
{
    protected IActionResult TurboRedirect(string url)            // 303 See-Other; 302 hangs under Turbo
    {
        Response.StatusCode = (int)System.Net.HttpStatusCode.SeeOther;
        Response.Headers.Location = url;
        return new EmptyResult();
    }
    protected IActionResult TurboInvalidPage()                   // 422 so Turbo swaps the body; 200 is ignored
    {
        Response.StatusCode = (int)System.Net.HttpStatusCode.UnprocessableEntity;
        return Page();
    }
}
```

---

### `Pages/Roles/Edit.cshtml.cs` (page model, request-response read+write)

**Analog (read/GET path + helpers + gating):** `Pages/Roles/Detail.cshtml.cs`
**Analog (POST/bind/validate path):** `Pages/Login.cshtml.cs`

**Class header + DI + route binding** (copy from `Detail.cshtml.cs:1-25`) — note `[Authorize]`, ctor-injected `IBacklotApiClient` + `ILogger<T>`, and `[BindProperty(SupportsGet = true)] string Uid`:
```csharp
[Authorize]
public class EditModel : TurboEditPageModel        // <-- inherit the NEW base, not AuthenticatedPageModel directly
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<EditModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string Uid { get; set; } = string.Empty;

    public EditModel(IBacklotApiClient api, ILogger<EditModel> logger) { _api = api; _logger = logger; }
}
```

**OnGetAsync pattern — SetUserContext + SafeApiCall + try/catch** (copy structure from `Detail.cshtml.cs:27-53`). Edit fetches BOTH schema and detail (RESEARCH.md Q5/Pattern 3):
```csharp
public async Task<IActionResult> OnGetAsync()
{
    SetUserContext();
    if (string.IsNullOrWhiteSpace(Uid)) return RedirectToPage("/Roles/Index");
    try
    {
        var (detail, r1) = await SafeApiCall(async () => await _api.GetRoleDetailAsync(Uid));
        if (r1 != null) return r1;
        var (schema, r2) = await SafeApiCall(async () => await _api.GetRoleSchemaAsync());
        if (r2 != null) return r2;
        // CanWrite gate (defense-in-depth, RESEARCH Security V4): reuse DetailModel.GetPermissions
        // match schema row by __Skills[0] == schema.Role (RESEARCH Pattern 3)
        // seed Fields dict from DetailModel.GetStringField(detail, f.Field)
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        _logger.LogWarning(ex, "Failed to load role for editing uid={Uid}", Uid);
        ErrorMessage = "Couldn't load this role for editing.";
    }
    return Page();
}
```

**Reuse static helpers from `DetailModel` directly — do NOT reimplement** (`Detail.cshtml.cs:64-114`):
- `DetailModel.GetStringField(detail, field)` — pull current value for each schema field (line 64-69)
- `DetailModel.GetSkills(detail)` — `__Skills[0]` for schema-row matching (line 71-81)
- `DetailModel.GetPermissions(detail)` — `CanWrite` gate (line 83-96)
- `DetailModel.GetPageTitle(detail)` — "Edit {RoleName}" title (line 111-114)

**Dynamic form binding** (RESEARCH Pattern 2, lines 169-188). Mirrors `Login.cshtml.cs:22-23` `[BindProperty]` style:
```csharp
[BindProperty] public Dictionary<string, string?> Fields { get; set; } = new();   // name="Fields[FieldName]"
[BindProperty] public new string Uid { get; set; } = string.Empty;                // hidden field round-trip (D-05)
```

**OnPostAsync — single save path, isvalid → 422 or persist → 303** (validation orchestration mirrors `Login.cshtml.cs:35-63` ModelState/early-return shape, but errors come from the API, not local DataAnnotations):
```csharp
public async Task<IActionResult> OnPostAsync()
{
    SetUserContext();
    // re-fetch schema (authoritative field list — never trust posted keys; RESEARCH Q5 + Security "mass assignment")
    // build payload from schema, skipping Calculated/read-only (BuildPayload, RESEARCH Pattern 2)
    var (outcome, r1) = await SafeApiCall(async () => await _api.ValidateRoleAsync(payload));
    if (r1 != null) return r1;
    if (outcome is null || !outcome.IsValid) { ValidationErrors = outcome?.Results ?? []; return TurboInvalidPage(); }   // 422
    var (_, r2) = await SafeApiCall(async () => await _api.PersistRoleAsync(payload));
    if (r2 != null) return r2;
    return TurboRedirect($"/roles/{Uid}?saved=1");   // 303
}
```

**Error-message property + catch pattern** (copy `Detail.cshtml.cs:18,46-50`): expose `string? ErrorMessage`, set inside `catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)`. For non-validation persist failure, surface the D-08-spec'd message and re-render with entered values (UI-SPEC "Persist failure" row).

---

### `Pages/Roles/Edit.cshtml` (view, schema-driven form)

**Analog (form + antiforgery + Turbo):** `Pages/Login.cshtml`
**Analog (page chrome: back link, uid row, error alert):** `Pages/Roles/Detail.cshtml`

**Page directive + ViewData activation** (copy `Detail.cshtml:1-7`) — route is `@page "/roles/{uid}/edit"`, set `ViewData["ActiveNav"] = "roles"`:
```cshtml
@page "/roles/{uid}/edit"
@model Backlot.Studio.Pages.Roles.EditModel
@{ ViewData["Title"] = Model.PageTitle; ViewData["ActiveNav"] = "roles"; }
```

**Form + antiforgery + Turbo-driven** (copy `Login.cshtml` form block — BUT do NOT copy `data-turbo="false"`; the edit form MUST be Turbo-driven, RESEARCH line 302):
```cshtml
<form method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="Uid" />
    ...
</form>
```
> Login.cshtml uses `data-turbo="false"`; the edit form is the opposite — it relies on Turbo to follow the 303 and swap on 422. Keep the `@Html.AntiForgeryToken()` (RESEARCH "Antiforgery under Turbo": token is a hidden field, serialized verbatim by Turbo, no extra config).

**Back link + Uid copy row** (copy `Detail.cshtml:9, 22-26`) — reuses `[data-action="copy-uid"]` JS already in `wwwroot/js/studio.js:67-71`, no new JS:
```cshtml
<a href="/roles/@Model.Uid" class="btn btn-link p-0 mb-3"><i class="bi bi-arrow-left me-1"></i>Back to @Model.PageTitle</a>
...
<code class="text-muted small">@Model.Uid</code>
<button data-action="copy-uid" data-uid="@Model.Uid" class="btn btn-link p-0" style="min-height:44px"><i class="bi bi-clipboard"></i></button>
```

**Error summary block** (copy/mirror `Detail.cshtml:11-17` `alert-danger` pattern; D-07 summary, UI-SPEC copy "Please fix the following before saving:"):
```cshtml
@if (Model.ValidationErrors.Any())
{
    <div class="alert alert-danger" role="alert">
        Please fix the following before saving:
        <ul class="mb-0">@foreach (var e in Model.ValidationErrors) { <li>@e.ErrorMessage</li> }</ul>
    </div>
}
```

**Per-field widget loop** — RESEARCH Pattern 2 (lines 190-212); widget mapping D-01 (`Boolean`→checkbox, `Int32`/`Decimal`/…→`type=number`, else→text), read-only iff `Characteristic == "Calculated"` (RESEARCH Q3). Field names shown as `<code>` to match Detail's field table.

---

### `Services/IBacklotApiClient.cs` (service contract) + `Services/BacklotApiClient.cs` (service impl)

**Analog:** existing methods in the same files — exact match.

**Interface additions** (mirror `IBacklotApiClient.cs:10-13` signatures, `CancellationToken ct = default` convention):
```csharp
Task<IReadOnlyList<RoleSchema>?> GetRoleSchemaAsync(CancellationToken ct = default);
Task<ValidationOutcome?> ValidateRoleAsync(object roleData, CancellationToken ct = default);
Task<JsonElement?> PersistRoleAsync(object roleData, CancellationToken ct = default);
```

**Implementation — copy `GetRoleDetailAsync` / envelope-helper pattern** (`BacklotApiClient.cs:17-29, 60-64`). GET for schema (like `GetEnvelopeAsync`), POST for isvalid/persist (like `PostEnvelopeAsync` with `PascalOptions`):
```csharp
public async Task<IReadOnlyList<RoleSchema>?> GetRoleSchemaAsync(CancellationToken ct = default)
{
    var envelope = await GetEnvelopeAsync<IReadOnlyList<RoleSchema>>("api/role/director/roles", ct);
    return envelope?.Body;
}
public async Task<ValidationOutcome?> ValidateRoleAsync(object roleData, CancellationToken ct = default)
{
    var envelope = await PostEnvelopeAsync<ValidationOutcome>("api/role/role/isvalid", roleData, ct);
    return envelope?.Body;
}
public async Task<JsonElement?> PersistRoleAsync(object roleData, CancellationToken ct = default)
{
    var envelope = await PostEnvelopeAsync<JsonElement>("api/role/persist/persist", roleData, ct);
    return envelope?.Body;
}
```
> Reuse the existing private `GetEnvelopeAsync<T>` / `PostEnvelopeAsync<T>` helpers and the static `PascalOptions` (`BacklotApiClient.cs:10`) — they already deserialize the `{Body,Status,TimeInMs,ExecutionTime}` envelope (`ApiEnvelope.cs:3-9`) in PascalCase. RESEARCH Pitfall 5 / A1: confirm `IsValid`/`Results` casing on first live run.

---

### `Models/Api/RoleSchema.cs` + `ValidationOutcome.cs` (DTOs)

**Analog:** `Models/Api/RoleFind.cs` (`FindResult` with `JsonElement[]` for dynamic data, line 26-32) and `Models/Api/ScenarioItem.cs` — same namespace `Backlot.Studio.Models.Api`, plain POCOs with default initializers.

**Schema DTOs** (shapes from RESEARCH Q3/sources — `director/roles` projection):
```csharp
namespace Backlot.Studio.Models.Api;

public class RoleSchema { public string Role { get; set; } = ""; public List<FieldSchema> Fields { get; set; } = []; public List<string> Skills { get; set; } = []; }
public class FieldSchema { public string Field { get; set; } = ""; public string Type { get; set; } = ""; public List<CharacteristicSchema> Characteristics { get; set; } = []; }
public class CharacteristicSchema { public string Characteristic { get; set; } = ""; /* Parameters omitted for v1 */ }
```

**Validation DTOs** (RESEARCH Q2, lines 256-269 — `{ IsValid, Results:[{ErrorMessage, MemberNames[]}] }`; parse defensively):
```csharp
public class ValidationOutcome { public bool IsValid { get; set; } public List<ValidationResultItem> Results { get; set; } = []; }
public class ValidationResultItem { public string? ErrorMessage { get; set; } public List<string>? MemberNames { get; set; } }
```
> Match the `FindResult` convention (`RoleFind.cs:26-32`): non-nullable collection props initialized to `[]`, `JsonElement` for any still-dynamic blob. `MemberNames` captured but unused in v1 (D-07 summary block); reserved for v2 inline-per-field.

---

### `Pages/Roles/Detail.cshtml.cs` + `Detail.cshtml` (MODIFY — success banner, D-08)

**Analog:** itself — add a `?saved=1` read mirroring the existing `[BindProperty(SupportsGet=true)] Uid` (`Detail.cshtml.cs:14-15`) and an `alert-success` block mirroring the existing `alert-danger` block (`Detail.cshtml:11-17`).

**PageModel change** (`Detail.cshtml.cs`): add
```csharp
[BindProperty(SupportsGet = true)]
public bool Saved { get; set; }
```

**View change** (`Detail.cshtml`, after line 7, mirror the `alert-danger` structure as `alert-success alert-dismissible`, UI-SPEC copy "Role saved."):
```cshtml
@if (Model.Saved)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        Role saved.
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```
> NO TempData (D-08 hard constraint). The flag arrives in the `Location` URL from `TurboRedirect("/roles/{uid}?saved=1")` and survives the 303 natively (RESEARCH Q4).

---

## Shared Patterns

### Authentication / 401 handling
**Source:** `Pages/AuthenticatedPageModel.cs:14-31`
**Apply to:** Edit page model (via the new `TurboEditPageModel` base) and the modified Detail.
Every handler calls `SetUserContext()` first, then wraps each API call in `SafeApiCall<T>()` and early-returns the redirect tuple. `SafeApiCall` catches `BacklotApiUnauthorizedException`, sets `Response.Headers["Turbo-Visit-Control"]="reload"` and redirects to `/Login`.

### Page-level API error handling
**Source:** `Pages/Roles/Detail.cshtml.cs:18, 34-52`
**Apply to:** Edit `OnGetAsync`/`OnPostAsync`.
```csharp
public string? ErrorMessage { get; private set; }
...
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    _logger.LogWarning(ex, "...", Uid);
    ErrorMessage = "Couldn't load this role for editing.";
}
```
Rendered as `<div class="alert alert-danger" role="alert">@Model.ErrorMessage</div>` (Detail.cshtml:11-17).

### Envelope-based API client calls
**Source:** `Services/BacklotApiClient.cs:17-29` (`GetEnvelopeAsync`/`PostEnvelopeAsync`) + `Services/ApiEnvelope.cs`
**Apply to:** all three new client methods. Reuse the private helpers + static `PascalOptions`; never `new HttpClient()` (CLAUDE.md constraint — typed client only).

### Layout activation
**Source:** `Pages/Roles/Detail.cshtml:5`
**Apply to:** Edit view — `ViewData["ActiveNav"] = "roles"` so `_Sidebar.cshtml` highlights Roles.

### Antiforgery under Turbo
**Source:** `Pages/Login.cshtml` (`@Html.AntiForgeryToken()` inside `<form method="post">`)
**Apply to:** Edit form — keep the token, but DROP `data-turbo="false"` (Login opts out of Turbo; Edit must stay Turbo-driven for the 303/422 contract). No `app.UseAntiforgery()` needed (Razor Pages auto-validates).

### Copy-UID affordance
**Source:** `Pages/Roles/Detail.cshtml:22-26` + `wwwroot/js/studio.js:67-71`
**Apply to:** Edit form Uid row (D-05). Reuse `[data-action="copy-uid"]` delegation — no new JS.

## No Analog Found

Files / capabilities with no close in-repo match (use RESEARCH.md patterns):

| File / Capability | Role | Data Flow | Reason |
|-------------------|------|-----------|--------|
| `TurboRedirect()` (303) + `TurboInvalidPage()` (422) bodies | base page model | request-response | No prior write/redirect path exists in Studio; this is the phase's isolated hazard. Use RESEARCH Pattern 1 (lines 138-161) verbatim; closest stylistic precedent is the direct `Response.Headers` write in `AuthenticatedPageModel.SafeApiCall` (line 28). Must be smoke-tested against the running API (RESEARCH Q1 / A4). |
| Dynamic `Dictionary<string,string?>` form binding | page model | transform | No existing form binds an open-ended dict; closest is Login's static `[BindProperty] LoginInputModel`. Use RESEARCH Pattern 2 (lines 169-213) — including the unchecked-checkbox `false`-defaulting gotcha (Pitfall 3). |

## Metadata

**Analog search scope:** `Pages/`, `Pages/Roles/`, `Services/`, `Models/Api/`, `wwwroot/js/`
**Files scanned:** `Detail.cshtml.cs`, `Detail.cshtml`, `AuthenticatedPageModel.cs`, `Login.cshtml.cs`, `Login.cshtml`, `BacklotApiClient.cs`, `IBacklotApiClient.cs`, `ApiEnvelope.cs`, `RoleFind.cs`, `ScenarioItem.cs`, `studio.js`
**Pattern extraction date:** 2026-06-23
