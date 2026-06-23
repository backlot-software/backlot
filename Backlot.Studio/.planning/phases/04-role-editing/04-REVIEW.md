---
phase: 04-role-editing
reviewed: 2026-06-23T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - Models/Api/RoleSchema.cs
  - Models/Api/ValidationOutcome.cs
  - Pages/Roles/Detail.cshtml
  - Pages/Roles/Detail.cshtml.cs
  - Pages/Roles/Edit.cshtml
  - Pages/Roles/Edit.cshtml.cs
  - Pages/TurboEditPageModel.cs
  - Services/BacklotApiClient.cs
  - Services/IBacklotApiClient.cs
findings:
  critical: 1
  warning: 6
  info: 4
  total: 11
status: issues_found
---

# Phase 04: Code Review Report

**Reviewed:** 2026-06-23
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

Reviewed the role-editing slice: schema/validation DTOs, the typed API client edit methods, the Turbo 303/422 base page model, and the Edit/Detail pages. The mass-assignment guard (`BuildPayload` driven by the server-refetched schema, not posted keys) and the server-side `CanWrite` re-check are sound and correctly designed. The Turbo 303/422 status contract is also correct for full-page Turbo Drive form submits.

However there are real correctness and security defects: a culture-sensitive numeric parse that silently corrupts decimal/float values on non-invariant locales (BLOCKER-class data integrity), an unencoded user-controlled value placed directly into the `Location` response header, a TOCTOU/permission-vs-validate ordering gap, and several robustness gaps in the API client and validation flow.

## Critical Issues

### CR-01: Culture-sensitive numeric parsing corrupts decimal/float values

**File:** `Pages/Roles/Edit.cshtml.cs:194-208`
**Issue:** `CoerceByType` parses `Decimal`, `Double`, `Single` (and all integer types) using the default-culture `TryParse` overloads. HTML number inputs and the round-trip from `CurrentValue` always use invariant formatting (`.` decimal separator), but the server's current culture decides how `decimal.TryParse("1.5", out var dec)` is interpreted. On any host configured with a comma-decimal locale (de-DE, nl-NL, fr-FR, etc.), `"1.5"` parses as `15` (the `.` is read as a thousands separator) or fails outright, and `"1,5"` would parse as `15`. The coerced value is then persisted via `PersistRoleAsync` — silent data corruption with no error surfaced to the user. This is environment-dependent and will not reproduce on an en-US dev box, making it a latent data-integrity bug.
**Fix:**
```csharp
using System.Globalization;

return type switch
{
    "Byte" => byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : (object?)raw,
    // ... apply NumberStyles + CultureInfo.InvariantCulture to every branch ...
    "Decimal" => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec) ? dec : (object?)raw,
    "Double" => double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (object?)raw,
    "Single" => float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var fl) ? fl : (object?)raw,
    _ => raw
};
```

## Warnings

### WR-01: User-controlled `Uid` written unencoded into the `Location` header

**File:** `Pages/Roles/Edit.cshtml.cs:136` and `Pages/TurboEditPageModel.cs:17-22`
**Issue:** `TurboRedirect($"/roles/{Uid}?saved=1")` interpolates the route-supplied `Uid` directly into the `Location` header via `Response.Headers.Location = url` with no URL-encoding. `Uid` is attacker-influencable (it is the value the user navigated with). While the `{uid}` route segment can't contain a raw `/`, it can contain characters that are special in a URL (`#`, `?`, `%`, spaces, backslashes) which produce a malformed redirect target, and on some pipelines header-injection guards are the only thing preventing a response-splitting attempt. Building redirect targets from raw user input is an open-redirect / header-integrity anti-pattern.
**Fix:** Encode the segment and avoid string interpolation for the location:
```csharp
return TurboRedirect($"/roles/{Uri.EscapeDataString(Uid)}?saved=1");
```
Better: redirect by route name (`Url.Page("/Roles/Detail", new { uid = Uid, saved = 1 })`) so the framework builds a well-formed, encoded URL, then pass that to `TurboRedirect`.

### WR-02: `SafeApiCall` only catches Unauthorized; other API failures escape unhandled in `Validate`/`Persist` chain

**File:** `Pages/AuthenticatedPageModel.cs:19-31` and `Pages/Roles/Edit.cshtml.cs:117-134`
**Issue:** `SafeApiCall` wraps only `BacklotApiUnauthorizedException`. `BacklotApiClient` calls `EnsureSuccessStatusCode()`, which throws `HttpRequestException` on any non-2xx (e.g., 400/403/404/500 from `isvalid` or `persist`). Those propagate out of `SafeApiCall` and are caught by the outer `catch (... HttpRequestException or TaskCanceledException)` in `OnPostAsync` — so far OK — but note the validate step: if the API returns a 422/400 *with a body* describing field errors, `EnsureSuccessStatusCode` throws and the structured validation results are discarded, collapsing into the generic "Save failed" banner. The user loses per-field feedback the API actually provided. The defensive `ValidationOutcome` parsing at lines 120-130 can only run when `isvalid` returns 2xx; a non-2xx validation response never reaches it.
**Fix:** In `ValidateRoleAsync` (and where the API signals validation via non-2xx), read the body before/instead of `EnsureSuccessStatusCode()` so the `ValidationOutcome` is still deserialized and surfaced through the 422 form path rather than the generic catch.

### WR-03: TOCTOU between `CanWrite` check, `isvalid`, and `persist`

**File:** `Pages/Roles/Edit.cshtml.cs:87-133`
**Issue:** `OnPostAsync` issues three separate API round-trips (detail for permission/skills, `isvalid`, `persist`). The `CanWrite` decision is made from the detail fetch, then validation and persistence happen in later calls. Between these calls permissions can change, and more importantly the local `CanWrite` gate is advisory — only the final `persist` enforcement is authoritative. The code comment claims defense-in-depth, but the local check adds an extra full role-detail fetch on every save purely to re-derive a flag the API must re-check anyway. If the detail fetch and persist disagree, the user sees inconsistent behavior. Functionally acceptable for v1, but the multi-fetch ordering is fragile and the local gate should not be treated as a security boundary.
**Fix:** Rely on the API's `persist`/`isvalid` permission enforcement as the authority; if a local pre-check is kept, document it as UX-only and handle the API's 403 explicitly (currently a 403 becomes a generic "Save failed" via `EnsureSuccessStatusCode`).

### WR-04: `GetEnvelopeAsync` / `PostEnvelopeAsync` deserialize with Web defaults but serialize with `General` — silent shape mismatch risk

**File:** `Services/BacklotApiClient.cs:17-29`
**Issue:** Sends use `PascalOptions` (`JsonSerializerDefaults.General`, PascalCase, case-sensitive), but `ReadFromJsonAsync<ApiEnvelope<T>>(...)` on lines 21 and 28 is called with **no options**, so it uses `JsonSerializerDefaults.Web` (camelCase policy, case-insensitive). It happens to work today because case-insensitive matching maps `Body`/`Status` correctly, but the two halves of the client use different serializer configurations. For nested PascalCase DTOs (`ValidationOutcome.Results[].ErrorMessage`, `MemberNames`) this relies entirely on case-insensitive fallback; any future option (e.g., enabling `PropertyNameCaseInsensitive = false` for performance) would silently break deserialization. The `ValidationOutcome` doc comment explicitly flags PascalCase as unconfirmed ("Confirm PascalCase casing on first live run") — yet deserialization quietly tolerates either casing, so a casing regression would go undetected.
**Fix:** Use one shared `JsonSerializerOptions` for both read and write, and pass it to `ReadFromJsonAsync` explicitly so casing behavior is intentional and consistent.

### WR-05: `EnsureSuccessStatusCode` discards API error bodies, defeating diagnostics

**File:** `Services/BacklotApiClient.cs:20, 27`
**Issue:** `response.EnsureSuccessStatusCode()` throws `HttpRequestException` with only the status code; the response body (which Backlot envelopes carry `Status`/diagnostic text in) is dropped. Combined with the page-level catch that logs `ex` but shows a generic message, operators get no insight into *why* a save or validation failed. For an admin/operator tool whose stated value is "edit without writing API calls by hand," swallowing the API's own error detail is a meaningful robustness regression.
**Fix:** On non-success, read `response.Content` and include it in the thrown exception message (and log it), e.g. throw a custom exception carrying status + body.

### WR-06: `MatchSchema` can bind the wrong schema when a role presents multiple skills

**File:** `Pages/Roles/Edit.cshtml.cs:148-164`
**Issue:** `MatchSchema` matches `__Skills[0]` against `schema.Role`, then falls back to "first skill that matches any schema row." If a role presents multiple skills and the primary skill has no schema row but two later skills do, the first arbitrary match wins. The chosen schema dictates which fields are editable and which are coerced/persisted, so a wrong match silently edits/persists the wrong field set. On GET this only mis-renders; on POST (lines 96, 115) it determines `BuildPayload`, so a mismatch can write fields under the wrong role contract.
**Fix:** Require an exact primary-skill match and treat the absence as an explicit "no editable schema" state rather than guessing from secondary skills; if multi-skill fallback is intended, document the precedence and verify the matched schema's fields are a subset the posted form actually rendered.

## Info

### IN-01: `WhoAmIAsync` return type `object?` is effectively unusable

**File:** `Services/BacklotApiClient.cs:39-43`, `Services/IBacklotApiClient.cs:9`
**Issue:** `GetEnvelopeAsync<object>` returns `Body` deserialized as `System.Text.Json.JsonElement` boxed in `object`. Any caller must downcast/re-parse. Prefer `JsonElement?` or a typed DTO for clarity.

### IN-02: `MemberNames` captured but unused; per-field errors collapse to a summary

**File:** `Models/Api/ValidationOutcome.cs:20-24`, `Pages/Roles/Edit.cshtml:28-39`
**Issue:** `ValidationResultItem.MemberNames` is parsed but never used; all validation errors render in a single summary list rather than against the offending field. Documented as a v2 item — acceptable for v1, noted for traceability.

### IN-03: Complex (object/array) field values rendered as raw JSON text

**File:** `Pages/Roles/Detail.cshtml.cs:104-115` and `Pages/Roles/Detail.cshtml:84-90`
**Issue:** `GetNonSystemFields` uses `prop.Value.ToString()` for non-string kinds, so nested objects/arrays render as minified JSON in the detail table. Edit handles only scalar inputs. Harmless for scalar roles but produces confusing output for roles with structured fields.

### IN-04: Misleading comment block in `OnPostAsync`

**File:** `Pages/Roles/Edit.cshtml.cs:83-86`
**Issue:** The comment ("Match by the posted skill carried via the schema; fall back to re-deriving from a detail fetch is unnecessary here...") contradicts the code immediately below it, which *does* fetch the detail to re-resolve. Stale/contradictory comment; clean up to match the implemented flow.

---

_Reviewed: 2026-06-23_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
