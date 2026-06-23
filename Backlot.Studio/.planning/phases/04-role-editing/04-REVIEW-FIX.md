---
phase: 04-role-editing
fixed_at: 2026-06-23T00:00:00Z
review_path: .planning/phases/04-role-editing/04-REVIEW.md
iteration: 1
findings_in_scope: 7
fixed: 7
skipped: 0
status: all_fixed
---

# Phase 04: Code Review Fix Report

**Fixed at:** 2026-06-23
**Source review:** .planning/phases/04-role-editing/04-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 7 (1 Critical + 6 Warning)
- Fixed: 7
- Skipped: 0

All in-scope findings were applied and each commit was verified with a successful
`dotnet build Backlot.Studio.csproj` (0 warnings, 0 errors).

## Fixed Issues

### CR-01: Culture-sensitive numeric parsing corrupts decimal/float values

**Files modified:** `Backlot.Studio/Pages/Roles/Edit.cshtml.cs`
**Commit:** e3c537b
**Applied fix:** Added `using System.Globalization;` and rewrote every branch of `CoerceByType`
to parse with explicit `NumberStyles` + `CultureInfo.InvariantCulture`. Integer types use
`NumberStyles.Integer`, `Decimal` uses `NumberStyles.Number`, and `Double`/`Single` use
`NumberStyles.Float | NumberStyles.AllowThousands`. The `.` decimal separator from HTML number
inputs is now interpreted consistently regardless of host culture, eliminating the comma-decimal
locale data-corruption path.

### WR-01: User-controlled `Uid` written unencoded into the `Location` header

**Files modified:** `Backlot.Studio/Pages/Roles/Edit.cshtml.cs`
**Commit:** bc1c9fa
**Applied fix:** Replaced the raw interpolated redirect (`/roles/{Uid}?saved=1`) with
`Url.Page("/Roles/Detail", new { uid = Uid, saved = 1 })`, which builds a framework-encoded URL,
falling back to `$"/roles/{Uri.EscapeDataString(Uid)}?saved=1"` if route resolution returns null.
Verified the Detail page route is `@page "/roles/{uid}"`, so `uid` becomes an encoded segment and
`saved` becomes a query param — matching the original target shape.

### WR-04 / WR-05 / WR-02: JSON option consistency, error-body preservation, validation-outcome survival

**Files modified:** `Backlot.Studio/Services/BacklotApiClient.cs`, `Backlot.Studio/Services/BacklotApiException.cs` (new)
**Commit:** f03b793
**Applied fix:** These three findings are co-located edits in the API client and were committed
together (they share the same method bodies and the new exception type):
- **WR-04:** Replaced the read/write option split with a single shared `JsonOptions`
  (`JsonSerializerDefaults.General`, `PropertyNameCaseInsensitive = true`) passed explicitly to
  both `PostAsJsonAsync` and every `ReadFromJsonAsync` call.
- **WR-05:** Introduced `BacklotApiException` carrying `StatusCode` + `ResponseBody`, and replaced
  `EnsureSuccessStatusCode()` with an `EnsureSuccessAsync` helper that reads the response body
  before throwing, so the API's diagnostic detail is preserved for logs/operators.
- **WR-02:** Rewrote `ValidateRoleAsync` so a client-validation failure (4xx that is not 401/403)
  still deserializes the `ApiEnvelope<ValidationOutcome>` from the error body, returning the
  structured outcome instead of collapsing into the generic "Save failed" banner. Auth and 5xx
  responses still throw.

### WR-03: TOCTOU between `CanWrite` check, `isvalid`, and `persist`

**Files modified:** `Backlot.Studio/Pages/Roles/Edit.cshtml.cs`, `Backlot.Studio/Services/BacklotApiException.cs`
**Commit:** aa6adc9
**Applied fix:** Re-based `BacklotApiException` on `HttpRequestException` (using the
`(message, inner, statusCode)` constructor) so existing `catch (HttpRequestException)` blocks
across the pages keep handling non-success responses unchanged. Added an explicit
`catch (BacklotApiException) when StatusCode is Forbidden or Unauthorized` branch in `OnPostAsync`
that surfaces a clear "You don't have permission" message if permissions change between the
detail fetch and the persist call. Reworded the `CanWrite` pre-check comment to document it as
UX-only, not a security boundary.

**Note:** This finding's `BacklotApiException` base-class change refines the file introduced in
commit f03b793; both commits build cleanly.

### WR-06: `MatchSchema` can bind the wrong schema when a role presents multiple skills

**Files modified:** `Backlot.Studio/Pages/Roles/Edit.cshtml.cs`, `Backlot.Studio/Pages/Roles/Edit.cshtml`
**Commit:** b8e1def
**Applied fix:** Removed the secondary-skill fallback from `MatchSchema`; it now matches the
primary skill (`__Skills[0]`) only and returns `null` when no schema row matches. Added an
explicit "No editable schema matches this role's primary skill" empty state to `Edit.cshtml` for
the GET path so a null schema renders a clear message rather than a blank page. The POST path
already treats a null schema as `TurboInvalidPage()` with a load-failure message.

**Logic note:** WR-06 changes matching behaviour (removes a fallback path). It compiles and the
null-schema states are handled in both GET and POST, but the precedence change should be confirmed
against a real multi-skill role during human/UAT verification.

---

_Fixed: 2026-06-23_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
