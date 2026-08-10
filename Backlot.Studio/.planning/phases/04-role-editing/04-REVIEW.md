---
phase: 04-role-editing
reviewed: 2026-06-24T16:00:00Z
depth: standard
files_reviewed: 3
files_reviewed_list:
  - Pages/Roles/Edit.cshtml.cs
  - Pages/Roles/Edit.cshtml
  - Pages/Roles/Detail.cshtml.cs
findings:
  critical: 0
  warning: 3
  info: 3
  total: 6
status: issues_found
---

# Phase 4: Code Review Report

**Reviewed:** 2026-06-24T16:00:00Z
**Depth:** standard
**Files Reviewed:** 3
**Status:** issues_found

## Summary

This review covers the phase-04 gap-closure (plan 04-04) that fixed the role edit form rendering zero fields. The substantive change re-anchors schema-row selection (`MatchSchema`) and the page title (`GetPageTitle`) on the role's "most-derived" `__Skills` entry instead of `__Skills[0]`. The functional symptom (zero fields) is resolved and UAT passes.

The fix is correct *for the runtime the team tested on*, but it trades one positional assumption (`__Skills[0]` is the role's own type — provably false) for a different, equally undocumented positional assumption (`__Skills[last]` is the role's own type). I traced the data source: `__Skills` is built from `Type.GetInterfaces()` (`Backlot.Core/Loader.cs:288-291`), and `Type.GetInterfaces()` ordering is **not contractually guaranteed by the .NET runtime**. The framework's own `Roles` scenario (`Backlot.Defaults/Scenarios/Configuration/Roles.cs:67-74`) treats skills as an unordered *set* — it `Union`s them and filters by name, never by position — which is strong in-repo evidence that position is not a reliable selector. The new code also reintroduces a softened form of the very hazard the prior WR-06 finding was meant to prevent: matching a *secondary* skill's schema row.

A deterministic, position-independent match is available and unused: the API already returns `RoleSchema.Skills` per row, and the role-name field is `schema.Role`. Matching on identity/membership rather than array position would remove the fragility entirely.

No critical issues. Three warnings (all robustness/correctness-risk, not active crashes) and three info items.

## Warnings

### WR-01: Schema selection depends on undocumented `Type.GetInterfaces()` ordering

**File:** `Pages/Roles/Edit.cshtml.cs:181-191` (and mirror at `Pages/Roles/Detail.cshtml.cs:122-125`)
**Issue:** `MatchSchema` reverse-iterates `__Skills` and `GetPageTitle` calls `LastOrDefault()`, both on the premise (stated verbatim in the new comments) that "Type.GetInterfaces() ... appends the role's own concrete name LAST." This is not true in general:

1. `Type.GetInterfaces()` returns interfaces in an order the .NET runtime explicitly does **not** guarantee. Code relying on a specific position relies on an implementation detail that can change across runtime versions/patches.
2. In `Backlot.Core/Loader.cs:288-291`, the role's own name is appended last **only** in the `if (roleType.IsInterface) interfaces.Add(roleType)` branch. For a concrete (proxied) role *class*, the own name is just one of the unordered `GetInterfaces()` entries — there is no append step, so "last" has no basis at all.
3. The framework's own `Roles` scenario (`Backlot.Defaults/.../Roles.cs:67-74`) treats skills as a set: it `Union`s skills and filters them by *name* (`"Role"`, `"Uid"`, `"Permission"`, `role.FriendlyName()`), never by index. That is the canonical, position-independent way to pick the role's own skill in this codebase.

Impact: if the ordering assumption fails for any role (different runtime, a role class rather than interface, or a future framework change), the form silently binds the wrong schema row or shows a base-marker page title — i.e. a regression back to "wrong/empty fields," but harder to diagnose because the code *looks* fixed.

**Fix:** Select by identity, not position. The role's own name is recoverable without array order. Derive the own-name the same way the framework does (exclude the known base markers), then match exactly:

```csharp
private static RoleSchema? MatchSchema(JsonElement detail, IReadOnlyList<RoleSchema> schemas)
{
    // Base markers are not concrete app roles; the role's own name is the skill
    // that is none of these (mirrors Backlot.Defaults Roles.cs:72).
    var baseMarkers = new HashSet<string>(
        new[] { "Role", "Uid", "Permission", "Persist" }, StringComparer.OrdinalIgnoreCase);

    var ownSkills = DetailModel.GetSkills(detail)
        .Where(s => !baseMarkers.Contains(s))
        .ToList();

    // Match the own-skill row deterministically; null when no own row is visible.
    return ownSkills
        .Select(skill => schemas.FirstOrDefault(
            r => string.Equals(r.Role, skill, StringComparison.OrdinalIgnoreCase)))
        .FirstOrDefault(r => r != null);
}
```

Apply the same base-marker filter in `GetPageTitle` instead of `LastOrDefault()`.

### WR-02: Reverse-iteration can match a *secondary* skill's schema row (reintroduces the WR-06 hazard)

**File:** `Pages/Roles/Edit.cshtml.cs:181-191`
**Issue:** The loop returns the first schema row that matches *any* `__Skills` entry, scanning from the end. The matched schema dictates which fields are editable/coerced/persisted (`BuildPayload`, line 197). If the role's own concrete name has **no** visible schema row — a real state, because `Roles.cs:86-89` filters rows to roles used in scenarios the *current user* can access — but some other (secondary) skill of the role *does* have a visible row, `MatchSchema` silently falls through and binds that secondary row. The form then renders and edits fields under the wrong role contract.

This is precisely the failure mode the previous WR-06 finding was written to prevent ("do not fall back to secondary skills; guessing from a later skill could write fields under the wrong role contract"). The 04-04 comment correctly argues WR-06's premise about `__Skills[0]` was wrong — but in removing the bad premise it also removed the protection, rather than re-implementing it correctly. The desired behavior (bind exactly the role's own row, else null) is achievable without scanning secondary skills (see WR-01 fix).

**Fix:** Resolve the role's own concrete name first (per WR-01), match only that name against `schema.Role`, and return null if the own row is absent. Do not iterate-and-accept-any-skill. This preserves the legitimate part of the 04-04 change (don't trust `__Skills[0]`) while restoring the WR-06 guarantee (never edit under a non-own contract).

### WR-03: `RoleSchema.Skills` is available for a deterministic match but unused

**File:** `Pages/Roles/Edit.cshtml.cs:181-191`; `Models/Api/RoleSchema.cs:11`
**Issue:** `RoleSchema` carries a `Skills` list populated by the API (`Roles.cs:67-74`, already pre-filtered to exclude base markers and the role's own name in that scenario's projection). The matching code ignores it entirely and relies on positional `__Skills` ordering. Using the structured `Skills`/`Role` data the API deliberately returns would make the match self-describing and order-independent, removing the need for the fragile reverse-walk. Leaving it unused both wastes the contract and forces the brittle approach flagged in WR-01/WR-02.

**Fix:** Match against the schema's own descriptors (`schema.Role`, and/or `schema.Skills`) using set membership against the role's `__Skills`, choosing the row that corresponds to the role's own concrete identity rather than the first positional hit. See WR-01 fix.

## Info

### IN-01: Stale/contradictory comment in `OnPostAsync`

**File:** `Pages/Roles/Edit.cshtml.cs:84-87`
**Issue:** The comment block reads: "Match by the posted skill carried via the schema; fall back to re-deriving from a detail fetch is unnecessary here because the schema row is identified by the same set of fields the form rendered. Re-resolve from the role detail so the schema row is matched the same way as on GET." It contradicts itself — first says the detail re-fetch is "unnecessary," then says to re-resolve from the role detail (which the code does). The leftover first half misdescribes the actual logic and will mislead future readers.
**Fix:** Delete the first two sentences; keep only the accurate statement that the schema row is re-resolved from a fresh detail fetch so POST matches GET.

### IN-02: `MatchSchema` does not disambiguate when multiple rows could match

**File:** `Pages/Roles/Edit.cshtml.cs:186`
**Issue:** `schemas.FirstOrDefault(...)` silently takes the first row for a given skill name. If `director/roles` ever returns two rows with the same `Role` (e.g. a named variant), the choice is arbitrary and undetectable. Low likelihood today, but worth a guard or a logged warning so a future duplicate doesn't bind silently.
**Fix:** Consider `SingleOrDefault` with a caught/logged exception, or log a warning when more than one row matches the resolved own-name, so the ambiguity surfaces rather than being masked.

### IN-03: Empty-state copy can mislead given WR-02

**File:** `Pages/Roles/Edit.cshtml:41-54`
**Issue:** The disambiguated copy is an improvement. But because `MatchSchema` (WR-02) can bind a *secondary* row instead of returning null, the "No editable schema is visible for this role" branch may not fire in exactly the cases it describes — the user could instead be shown a populated form for the wrong contract, which no copy can warn about. The copy is only fully accurate once WR-01/WR-02 make null mean "own row truly absent."
**Fix:** No view change needed; resolving WR-01/WR-02 makes this copy correct. Tracked here only so the empty-state wording and the matching logic stay consistent.

---

_Reviewed: 2026-06-24T16:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
