# Phase 3: role-browsing-detail - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-22
**Phase:** 03-role-browsing-detail
**Areas discussed:** Search field target, Role Type column source, System field visibility

---

## Search field target

| Option | Description | Selected |
|--------|-------------|----------|
| Name field only | Criteria: [{Field: 'Name', Condition: 'Contains', Value}] | |
| Name + Uid (both) | Two Criteria entries OR-ed | |
| Let user pick the field | Add a second input | ✓ |

**User's choice:** Let user pick the field — single input with `field:value` syntax

| Sub-option | Description | Selected |
|------------|-------------|----------|
| Text + field name input | Two inputs side by side | |
| Field + condition dropdown + value | Three inputs | |
| `field:value` syntax | Single input, parse colon | ✓ |

**Condition / fallback:**

| Option | Description | Selected |
|--------|-------------|----------|
| Contains + fallback to Name | With colon: Contains; no colon: search Name | ✓ |
| Equals + fallback to Uid | Strict match | |
| Contains + fallback to Uid | Contains on Uid | |

**Notes:** User confirmed Contains condition throughout. Without colon, fall back to searching both Name and Uid. Placeholder text updated to communicate syntax: `"Name:John or Uid:abc123 or plain text"`.

---

## Role Type column source

**Primary question (type column source):**

| Option | Description | Selected |
|--------|-------------|----------|
| Read from dynamic result fields | Check for __Type or Type system field | |
| Remove the Type column | Show Uid + Name + Actions only | |
| Use For parameter as Type label | Only works when filtering by type | |

**User's choice (freeform):** `__Skills` array contains the role types. Default columns: Uid, Name, LastModified. Make column config per-skill configurable via localStorage.

**Config UX:**

| Option | Description | Selected |
|--------|-------------|----------|
| Gear icon on list page | Inline panel with checkboxes per skill type | ✓ |
| Editable column headers | Click to pick field | |
| Separate settings page | /roles/settings | |

**Mixed view:**

| Option | Description | Selected |
|--------|-------------|----------|
| Default columns for mixed view | Uid + Name + LastModified always | ✓ |
| First __Skill as type key | Per-row column set | |
| Union all columns | All configured columns | |

**Related roles:**

| Option | Description | Selected |
|--------|-------------|----------|
| Info + Uid + View button only | No Role Type column | ✓ |
| Call seekbase/detail per relation | N+1 fetch | |
| Parse type from Info string | Fragile | |

**Notes:** User decided related roles table shows only Uid (truncated + copy), Info string, and View button. Type not shown.

---

## System field visibility

| Option | Description | Selected |
|--------|-------------|----------|
| Show all fields | Every key rendered | |
| Hide all __ except __Permission and __Skills | Cleaner but loses debug info | |
| Show __Permission + __Skills + __LastModifiedDate | Hide all other __ | ✓ |

**User's choice:** Show `__Permission` (badges), `__Skills` (badges), `__LastModifiedDate` (raw ISO string). Hide all other `__` prefixed fields. All non-`__` fields shown as plain rows.

**__LastModifiedDate format:**

| Option | Description | Selected |
|--------|-------------|----------|
| Formatted date string | yyyy-MM-dd HH:mm | |
| Relative time | "2 hours ago" | |
| Raw ISO string | Exact API value | ✓ |

**Notes:** Developer tool context — raw ISO is most useful for debugging. All system fields on detail page are read-only.

---

## Claude's Discretion

- C# model types for dynamic role data
- Razor Page route structure for `/roles/{uid}/relations`
- Exact localStorage key schema for column config
- Turbo Frame URL construction for search/pagination
- `For` parameter value for all-roles query
- UID truncation implementation

## Deferred Ideas

None — discussion stayed within phase scope.
