# Phase 4: role-editing - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 04-role-editing
**Areas discussed:** Field widget rendering, Editable field set, Validation timing & errors
**Area declined for discussion (left to research):** Turbo save mechanics (303/422/antiforgery)

---

## Field Widget Rendering

### How should form inputs be rendered from the field schema in v1?

| Option | Description | Selected |
|--------|-------------|----------|
| Thin generic (all text) | Every editable field a text input regardless of Type | |
| Minimal typed | Mostly text, but handle obvious cases (bool→checkbox, multi-line→textarea) | ✓ |
| Full typed widgets | Read Type + Characteristics for number/date/checkbox/dropdown | |

**User's choice:** Minimal typed

### For 'minimal typed', how should numeric and unknown field types be handled?

| Option | Description | Selected |
|--------|-------------|----------|
| Number→number input | bool→checkbox, numeric→number input, everything else/unknown→text | ✓ |
| Numbers stay text | Only bool→checkbox and long string→textarea; numbers as text | |
| You decide | Pick during planning from real API Type values | |

**User's choice:** Number→number input

### What should trigger a textarea (multi-line)?

| Option | Description | Selected |
|--------|-------------|----------|
| Schema Type signals it | Textarea only for a long-text Type | |
| Length characteristic | Textarea when a max-length hint allows long values | |
| Skip textarea for v1 | All strings single-line text inputs | ✓ |

**User's choice:** Skip textarea for v1

**Notes:** Net widget mapping for v1 — bool→checkbox, numeric→`<input type=number>`, everything else (incl. unknown Type)→single-line text. No textarea, date pickers, or dropdowns. Aligns with the STATE.md blocker steer toward a thin form for v1.

---

## Editable Field Set

### Which fields should appear as editable inputs on the form?

| Option | Description | Selected |
|--------|-------------|----------|
| Schema fields only | One input per director/roles schema field, pre-filled from detail | ✓ |
| All non-__ detail fields | Every non-__ field on the role detail, schema or not | |
| You decide | Pick during planning once schema/detail alignment is known | |

**User's choice:** Schema fields only

### Should any schema fields be shown but locked (read-only)?

| Option | Description | Selected |
|--------|-------------|----------|
| Honor read-only characteristic | Read-only/key/computed fields rendered as disabled display | ✓ |
| All editable in v1 | Every schema field editable; rely on server to reject | |
| You decide | Decide during planning based on live Characteristics | |

**User's choice:** Honor read-only characteristic

### How should the role's Uid appear on the edit form?

| Option | Description | Selected |
|--------|-------------|----------|
| Visible read-only + hidden field | Shown read-only with copy button AND carried as hidden field | ✓ |
| Hidden field only | Carried as hidden field, not displayed | |
| You decide | Pick during planning / UI-SPEC | |

**User's choice:** Visible read-only + hidden field

**Notes:** Researcher must confirm the exact Characteristic name/signal that marks a field read-only/key/computed in the live `director/roles` response.

---

## Validation Timing & Errors

### When should role/isvalid run, relative to the save?

| Option | Description | Selected |
|--------|-------------|----------|
| On submit, before persist | Single handler: isvalid → 422 re-render if invalid, else persist → 303 | ✓ |
| Live on blur + on submit | Debounced per-field isvalid plus authoritative on-submit check | |
| You decide | Pick based on isvalid latency/shape | |

**User's choice:** On submit, before persist

### How should validation errors be displayed (isvalid shape unknown)?

| Option | Description | Selected |
|--------|-------------|----------|
| Inline + summary fallback | Map to fields inline; unmappable → top summary alert | |
| Inline only | Assume every error has a field name | |
| Summary block only | All errors in one top-of-form alert block | ✓ |

**User's choice:** Summary block only

### EDIT-02 specifies inline-next-to-field errors; summary-only deviates. Resolve how?

| Option | Description | Selected |
|--------|-------------|----------|
| Keep summary-only for v1 | Accept the deviation; record as conscious decision | ✓ |
| Inline + summary fallback after all | Honor EDIT-02 fully | |
| Summary now, inline if shape allows | Upgrade to inline if research finds clean field mapping | |

**User's choice:** Keep summary-only for v1

### After a successful save redirects (303) to detail, should the user get a confirmation?

| Option | Description | Selected |
|--------|-------------|----------|
| Flash success message | "Role saved" banner via TempData | (chosen, minus TempData) |
| Silent redirect | No banner; fresh data implies success | |
| You decide | Pick during planning / UI-SPEC | |

**User's choice:** Show a "Role saved" success banner on the detail page — **explicitly: do NOT use TempData. Use TurboJS and Razor Pages best practices.**

**Notes:** "No TempData" is a hard constraint. Mechanism left to researcher — e.g., redirect query flag read by the detail page, a Turbo Stream, or a `turbo:submit-end` client toast.

---

## Claude's Discretion

- **Turbo save mechanics** — full-page form vs turbo-frame; producing 303-success and 422-invalid responses that survive a prior Turbo navigation; antiforgery token flow under Turbo. The user deliberately left this area out of discussion as a research item (it is the central hazard this phase isolates; validate against the real app early per STATE.md blocker).
- Exact success-banner mechanism (Turbo/Razor-idiomatic, non-TempData).
- C# binding model for dynamic schema + values; schema-to-role matching (likely `__Skills[0]` ↔ schema `Role`).
- Non-validation persist failure handling (HTTP 500 / network) — surface error, preserve entered values.

## Deferred Ideas

- Full typed widgets / schema-aware widget hints from `Characteristics` → v2 ADV-01.
- Inline-per-field validation errors (literal EDIT-02 phrasing) and live/on-blur validation → follow-up after v1.
