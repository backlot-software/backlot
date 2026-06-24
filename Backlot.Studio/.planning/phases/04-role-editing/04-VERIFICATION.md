---
phase: 04-role-editing
verified: 2026-06-24T00:00:00Z
status: human_needed
score: 8/11 must-haves verified
behavior_unverified: 3
overrides_applied: 0
re_verification:
  previous_status: human_needed
  previous_score: 7/11
  gaps_closed:
    - "UAT BLOCKER (Test 1): Edit button permanently disabled — root cause (seekbase/detail Role-wrapper nesting) fixed via defensive UnwrapRoleDetail in GetRoleDetailAsync; CanWrite now reads __Permission at the correct level so the Edit button enables for writable roles"
    - "Always-false CanCreate badge removed from Detail.cshtml (framework never emits the property); CanRead/CanWrite badges retained"
    - "CR-01 locale defect (CoerceByType culture-sensitive numeric parse) fixed — now parses with CultureInfo.InvariantCulture, so '1.5' persists as 1.5 on comma-decimal locales; the locale-dependent half of the mass-assignment/coercion truth is now correct by inspection (commit e3c537b)"
  gaps_remaining:
    - "Live authenticated Turbo 303/422 round-trip (ROADMAP SC#3) — still needs a running, authenticated Backlot API; no automated test, host stores hashed-only password"
    - "Live isvalid response shape/casing confirmation — DTO PascalCase + 4xx-recovery (WR-02) by inspection, not confirmed against a real authenticated response"
    - "Live persist/persist round-trip + 'Role saved.' banner end-to-end — wired and read-verified, not exercised against live auth"
  regressions: []
behavior_unverified_items:
  - truth: "Submitting invalid data returns HTTP 422 and re-renders the form body in place; submitting valid data returns HTTP 303 to /roles/{uid} (surviving a prior Turbo navigation + antiforgery round-trip)"
    test: "Against a running Backlot API with valid Basic Auth: from a writable role's detail page click Edit (Turbo Drive nav), submit clearly-invalid data, then submit valid data; watch the network tab."
    expected: "Invalid POST -> HTTP 422 with the form body re-rendered IN PLACE (Turbo swaps body). Valid POST -> HTTP 303 with Location /roles/{uid}?saved=1 and Turbo navigates to the detail page. No 400 antiforgery failure."
    why_human: "Status codes are set correctly in TurboEditPageModel (303 SeeOther + Location, 422 + Page()) and verified by read, but whether Turbo Drive actually follows the 303 and swaps on 422 after a prior Turbo nav + the antiforgery round-trip is runtime client-server behavior. No test exercises it; the live smoke test cannot run because the demo host stores only a hashed password (no recoverable plaintext for Basic Auth)."
  - truth: "Submitting invalid data calls role/isvalid first, re-renders (422) with a summary block listing the REAL validation errors, and does NOT call persist"
    test: "Submit invalid data and confirm the summary block lists the REAL isvalid error messages (not the generic 'Validation failed.' fallback), and persist is not called (no state change in API logs)."
    expected: "role/isvalid returns IsValid=false with Results[]; persist/persist is NOT called; 'Please fix the following before saving:' lists each real ErrorMessage. The generic fallback only fires if the API returns invalid with empty Results."
    why_human: "isvalid-before-persist ordering is statically correct (persist at cs:136 is unreachable until IsValid passes at cs:123). ValidateRoleAsync now recovers a structured ValidationOutcome from 4xx bodies (WR-02, cs:142-158). But the live isvalid response casing/shape (RESEARCH A1, PascalCase assumed) was never confirmed against a real authenticated response; if the live casing differs the summary silently shows the generic fallback. No test; credential limitation."
  - truth: "Submitting valid data calls persist/persist and on success redirects (303) to /roles/{uid}, where a green 'Role saved.' banner appears"
    test: "Save a valid edit and confirm you land on /roles/{uid}?saved=1 with the green dismissible 'Role saved.' banner reflecting the persisted change; then visit /roles/{uid} directly (no flag) and confirm no banner."
    expected: "303 to /roles/{uid}?saved=1; Detail binds Saved=true and renders alert-success; the field values reflect the persisted change."
    why_human: "The ?saved=1 -> bool Saved -> banner wiring is statically verified, and the redirect target is built encoded via Url.Page (WR-01). That persist/persist actually stores the change AND the round-trip to the banner works end-to-end is runtime behavior. No test; credential limitation."
human_verification:
  - test: "Against a running Backlot API with valid Basic Auth: open a WRITABLE role's detail page and confirm the Edit button renders as an enabled link (the previously-blocking UAT Test 1). Click it, submit invalid data, then submit valid data; watch the network tab."
    expected: "Edit button enabled. Invalid -> HTTP 422 + form body re-rendered in place. Valid -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail with the green 'Role saved.' banner. No 400 antiforgery error. (Confirms ROADMAP SC#3 and that the 04-03 unwrap fix resolves UAT Test 1 against live auth.)"
    why_human: "Runtime Turbo client-server behavior surviving a prior Turbo nav + antiforgery; no test; live smoke test blocked by the demo host's hashed-only password. The 04-03 fix is proven by code inspection + a deterministic unwrap harness (WRAPPED/FLAT/NON-OBJECT all PASS), but the end-to-end live render of the wrapped response shape was never exercised under real auth."
  - test: "Submit invalid data and confirm the top-of-form summary block lists the REAL isvalid error messages (not the generic 'Validation failed.' fallback); confirm persist is not called."
    expected: "Real per-error ErrorMessage strings rendered in the alert-danger summary; no persisted change."
    why_human: "Live isvalid response casing/shape (RESEARCH A1) never confirmed against an authenticated response; DTO is PascalCase on assumption (4xx-recovery added in WR-02). No test; credential limitation."
---

# Phase 4: Role Editing Verification Report

**Phase Goal:** A user can edit any writable role through a schema-driven form, see inline validation feedback, and save changes — completing the final Core Value pillar (mutate) while isolating the Turbo form hazards in one place.
**Verified:** 2026-06-24
**Status:** human_needed
**Re-verification:** Yes — after gap closure (04-03 fixed the UAT Edit-button blocker; subsequent code-review fixes WR-01/02/03/06 + CR-01)

## Goal Achievement

This re-verification confirms that the gap-closure plan 04-03 fixed the UAT blocker at the code level and that two further behavior-unverified concerns from the prior verification have been resolved in code:

1. **UAT Test 1 blocker — Edit button permanently disabled — FIXED.** Root cause was that `seekbase/detail` nests the role under a `Role` wrapper while every `DetailModel.Get*` helper and the Edit-page CanWrite gate read `__Permission`/`__Skills`/fields at the top level. The fix adds a defensive `UnwrapRoleDetail` helper inside `GetRoleDetailAsync` (the single chokepoint), so all three call sites (`Detail.OnGet`, `Edit.OnGet`, `Edit.OnPost`) now read at the correct level with no PageModel change. Verified by read (BacklotApiClient.cs:85-106) and by the recorded deterministic unwrap harness (WRAPPED/FLAT/NON-OBJECT all PASS). `CanWrite` is no longer permanently false, so the Edit button renders as an enabled link for writable roles (Detail.cshtml:35-42).

2. **CanCreate always-false badge — REMOVED.** `grep CanCreate Detail.cshtml` returns 0; CanRead/CanWrite badges retained; `Backlot.Core` untouched (git status clean).

3. **CR-01 locale defect — FIXED (upgrade from prior behavior_unverified item #4).** The prior verification flagged `CoerceByType` using culture-sensitive `TryParse` as a latent data-integrity defect on comma-decimal locales. The current code (Edit.cshtml.cs:208-224, commit e3c537b) parses every numeric with `CultureInfo.InvariantCulture` and explicit `NumberStyles`, so `1.5` persists as `1.5` regardless of host culture. The locale-dependent half of the mass-assignment/coercion truth is now correct by inspection. This truth (#9) is upgraded from PRESENT_BEHAVIOR_UNVERIFIED to VERIFIED (its mass-assignment guard was already read-verified; the remaining concern was locale parsing, now fixed).

The build is green (`dotnet build Backlot.Studio` — 0 warnings, 0 errors). All static structure — schema-driven form, isvalid-before-persist ordering, 303/422 status setting, mass-assignment-safe schema-driven payload, TempData-free ?saved=1 banner, persist-failure value preservation, encoded redirect (WR-01), API-authoritative permission handling (WR-03), 4xx validation recovery (WR-02), primary-skill-only schema match (WR-06) — is correct by read.

The phase remains `human_needed` (not `passed`) because three behavior-dependent truths assert runtime Turbo write-form behavior against a live authenticated API that no test exercises and that still cannot be smoke-tested (the demo host stores only a hashed password). This is a verification limitation, not an implementation gap — and it is the same carried-forward STATE blocker A1/A4 recorded honestly by all three executors.

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Navigate to /roles/{uid}/edit and see a form with all editable fields rendered from director/roles schema, pre-filled from seekbase/detail | ✓ VERIFIED | `@page "/roles/{uid}/edit"`; OnGetAsync fetches GetRoleDetailAsync (now unwrapped) + GetRoleSchemaAsync, MatchSchema by primary skill, seeds Fields from DetailModel.GetStringField (Edit.cshtml.cs:35-71); view loops Model.SchemaFields (Edit.cshtml:61) |
| 2 | Bool->checkbox, numeric->number, everything else->text | ✓ VERIFIED | Edit.cshtml:71-91 three editable widget branches; IsBool/IsNumeric helpers (Edit.cshtml.cs:234-243); widget grep=4 |
| 3 | A Calculated field renders disabled, not editable | ✓ VERIFIED | IsReadOnly checks Characteristic=="Calculated" exactly (cs:231-232); disabled bg-light input + "Read-only" hint (Edit.cshtml:64-69); Required/StringLength/Range NOT treated as read-only |
| 4 | Uid read-only at top with copy button, carried in a hidden field | ✓ VERIFIED | Edit.cshtml:20-26 copy row (data-action=copy-uid) + "cannot be changed" hint; hidden `<input asp-for="Uid">` (Edit.cshtml:59) |
| 5 | Submitting invalid -> HTTP 422 + re-render in place; valid -> HTTP 303 to /roles/{uid} (after a prior Turbo nav) | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | TurboInvalidPage sets 422, TurboRedirect sets 303 SeeOther + Location (TurboEditPageModel — verified by read). Status-code SETTING verified; Turbo client FOLLOW behavior + antiforgery round-trip never runtime-tested (credential limitation, no test) |
| 6 | Invalid data calls isvalid first, 422 + summary block of REAL errors, does NOT call persist | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Ordering read-verified: isvalid at cs:120, 422 return at cs:133 BEFORE persist at cs:136. ValidateRoleAsync recovers structured outcome from 4xx (WR-02, cs:142-158). Summary block present (Edit.cshtml:28-39). Live isvalid casing/shape (A1) unconfirmed; no test |
| 7 | Valid data calls persist, 303 to /roles/{uid}, green "Role saved." banner | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | persist at cs:136; TurboRedirect to encoded /roles/{uid}?saved=1 via Url.Page (WR-01, cs:143-145); Detail binds bool Saved (Detail.cshtml.cs:20-21), renders alert-success (Detail.cshtml:20-26). End-to-end persist round-trip never runtime-tested |
| 8 | Success banner driven by ?saved=1 query flag read by detail page — no TempData | ✓ VERIFIED | [BindProperty(SupportsGet=true)] public bool Saved (Detail.cshtml.cs:20-21); banner gated on Model.Saved; grep TempData=0 in all 4 files |
| 9 | Persist payload built from schema field list, skipping Calculated, type-coerced locale-safely (mass-assignment safe) | ✓ VERIFIED | BuildPayload iterates Schema.Fields, never posted keys, skips IsReadOnly, seeds only Uid + schema fields (cs:184-194). CoerceByType now parses with InvariantCulture + explicit NumberStyles (cs:208-224, CR-01 fixed e3c537b) — locale corruption resolved |
| 10 | Non-validation persist failure (500/network) re-renders form with entered values + error message | ✓ VERIFIED | catch (HttpRequestException or TaskCanceledException) sets ErrorMessage + TurboInvalidPage (cs:157-162); Fields stay bound; alert-danger renders ErrorMessage (Edit.cshtml:10-16). Also a Forbidden/Unauthorized catch (WR-03, cs:147-156) |
| 11 | EDIT-02 pre-save validation errors shown (D-07: summary block, not inline) | ✓ VERIFIED | Summary block "Please fix the following before saving:" lists each ErrorMessage (Edit.cshtml:28-39). D-07 is a user-approved conscious deviation from the "inline" wording (CONTEXT.md:29; DISCUSSION-LOG.md:105 "User's choice: Summary block only") — verification treats summary-block as intended scope |

**Score:** 8/11 truths verified (3 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Pages/TurboEditPageModel.cs` | 303/422 helpers, base class | ✓ VERIFIED | TurboRedirect (SeeOther/303 + Location), TurboInvalidPage (422 + Page()) |
| `Pages/Roles/Edit.cshtml.cs` | EditModel : TurboEditPageModel, OnGet/OnPost | ✓ VERIFIED | OnGet (unwrapped detail + schema + seed), OnPost (isvalid->422 / persist->303), BuildPayload, CoerceByType (InvariantCulture), MatchSchema (primary-skill-only), IsReadOnly/IsBool/IsNumeric |
| `Pages/Roles/Edit.cshtml` | Schema-driven Turbo form, antiforgery, widget loop, Uid row | ✓ VERIFIED | @page route, @Html.AntiForgeryToken(), hidden Uid, per-type widgets, copy row, summary block, two "Nothing to edit" empty states, 1 btn-primary; no data-turbo=false, no Html.Raw |
| `Services/BacklotApiClient.cs` | 3 new methods + defensive UnwrapRoleDetail | ✓ VERIFIED | GetRoleSchemaAsync / ValidateRoleAsync (4xx-recovery) / PersistRoleAsync; UnwrapRoleDetail applied in GetRoleDetailAsync (Body.Role when present, else Body; cloned) |
| `Services/IBacklotApiClient.cs` | 3 new signatures | ✓ VERIFIED | All three declared; UnwrapRoleDetail kept private (no interface change) |
| `Models/Api/RoleSchema.cs` | RoleSchema/FieldSchema/CharacteristicSchema | ✓ VERIFIED | Three POCOs, collections default to [] |
| `Models/Api/ValidationOutcome.cs` | ValidationOutcome/ValidationResultItem | ✓ VERIFIED | PascalCase IsValid/Results/ErrorMessage/MemberNames |
| `Pages/Roles/Detail.cshtml(.cs)` | Saved query-flag + alert-success banner; no CanCreate badge | ✓ VERIFIED | public bool Saved bind + dismissible alert-success "Role saved." gated on Model.Saved; CanCreate badge removed (grep=0), CanRead/CanWrite retained |

### Key Link Verification

| From | To | Via | Status |
|------|-----|-----|--------|
| Edit.cshtml.cs | BacklotApiClient.cs | OnGet GetRoleSchemaAsync+GetRoleDetailAsync; OnPost ValidateRoleAsync+PersistRoleAsync | ✓ WIRED (cs:44,47,88,120,136) |
| Edit.cshtml.cs | TurboEditPageModel.cs | inherits; OnPost returns TurboRedirect/TurboInvalidPage | ✓ WIRED |
| BacklotApiClient.cs | Detail/Edit PageModels | GetRoleDetailAsync now returns UNWRAPPED role -> GetPermissions(...).CanWrite reads __Permission at correct level | ✓ WIRED (UnwrapRoleDetail at cs:89,97-106; consumed Detail.cshtml.cs:48, Edit.cshtml.cs:54,99) |
| Edit.cshtml.cs | Detail.cshtml.cs | TurboRedirect /roles/{uid}?saved=1 sets the flag Detail reads | ✓ WIRED (encoded Url.Page at cs:143 -> bool Saved at Detail.cshtml.cs:21) |
| Edit.cshtml | Edit.cshtml.cs | view renders Model.ValidationErrors | ✓ WIRED (Edit.cshtml:28-39) |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Edit.cshtml | Model.SchemaFields | GetRoleSchemaAsync -> GET director/roles | Yes (live API) | ✓ FLOWING |
| Edit.cshtml | Fields (pre-fill) | GetRoleDetailAsync (UNWRAPPED) -> POST seekbase/detail, seeded via GetStringField | Yes (live API; now reads under Body.Role) | ✓ FLOWING |
| Detail.cshtml | Model.CanWrite | GetPermissions(unwrapped __Permission).CanWrite | Yes (unwrap fix) | ✓ FLOWING (was DISCONNECTED pre-04-03) |
| Edit.cshtml | Model.ValidationErrors | ValidateRoleAsync -> POST role/isvalid Body | Live API (casing unconfirmed) | ⚠️ STATIC fallback exists; real-data path runtime-unverified |
| Detail.cshtml | Model.Saved | ?saved=1 query flag bound on GET | Yes (query bind) | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Studio builds | `dotnet build Backlot.Studio/Backlot.Studio.csproj` | Build succeeded, 0 Warning(s), 0 Error(s) | ✓ PASS |
| CanCreate badge removed | `grep -c CanCreate Detail.cshtml` | 0 | ✓ PASS |
| Backlot.Core untouched | `git status --short Backlot.Core/` | empty | ✓ PASS |
| UnwrapRoleDetail present + applied | `grep -c UnwrapRoleDetail BacklotApiClient.cs` | 3 | ✓ PASS |
| Unwrap logic correctness | scratch harness (04-03 SUMMARY): WRAPPED/FLAT/NON-OBJECT | ALL PASS, exit 0 | ✓ PASS |
| No TempData / no Html.Raw | grep across 4 files | TempData=0, Html.Raw=0 | ✓ PASS |
| Authenticated edit round-trip (303/422) | would require valid Basic Auth | Host stores hashed password only | ? SKIP — routed to human verification |

### Probe Execution

No probes declared and no `scripts/*/tests/probe-*.sh` present (Razor Pages UI phase). Step 7c not applicable.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| EDIT-01 | 04-01, 04-03 | Form with all editable fields dynamically rendered from director/roles schema | ✓ SATISFIED | Truths 1-4; unwrap fix (04-03) unblocks the Edit GET path + CanWrite gate |
| EDIT-02 | 04-02, 04-03 | Pre-save validation errors from role/isvalid shown (literal: "inline next to fields") | ✓ SATISFIED (D-07 deviation) | Truth 11; summary block. REQUIREMENTS.md/ROADMAP SC#2 say "inline" but D-07 is a recorded, user-approved deviation (CONTEXT.md:29, DISCUSSION-LOG.md:105). Treated as intended scope per the documented decision |
| EDIT-03 | 04-02, 04-03 | Save via persist/persist; success->redirect to detail; failure->errors re-displayed | ✓ SATISFIED (structure) / behavior-unverified (runtime) | Truths 7,10; 303 redirect + banner + persist-failure handling wired; live persist round-trip routed to human verification |

All three declared requirement IDs (EDIT-01, EDIT-02, EDIT-03) are accounted for and map to Phase 4 in REQUIREMENTS.md. Plan 04-03 re-declares all three (gap-closure). No orphaned requirements.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| (none) | — | TBD/FIXME/XXX debt markers | — | grep across all phase-04 source files: 0 |
| (none) | — | TempData / Html.Raw | — | 0 in all 4 edit/detail files (D-08 honored, XSS-safe) |
| (none) | — | culture-sensitive numeric parse (was 04-REVIEW CR-01) | ✓ RESOLVED | CoerceByType now uses CultureInfo.InvariantCulture (commit e3c537b) — no longer a warning |
| (none) | — | Stub returns / hardcoded empty render data | — | All UI data flows from live API; Known Stubs: None |

No BLOCKER-class issues. The single correctness warning from the prior verification (CR-01 culture-parse) is now fixed.

### Human Verification Required

Two items (see frontmatter `human_verification`), both stemming from the same root limitation — the live authenticated Turbo round-trip cannot be exercised because the demo host stores only a hashed password:

1. **Edit-button-enabled + Turbo 303/422 round-trip (re-runs the previously-blocking UAT Test 1)** — against a running API with valid Basic Auth, confirm the Edit button is enabled on a writable role, then submit invalid (expect 422 + in-place body swap) and valid (expect 303 -> /roles/{uid}?saved=1 + Turbo nav + green "Role saved." banner); confirm no 400 antiforgery failure. This is the live confirmation that the 04-03 unwrap fix resolves the UAT blocker end-to-end.
2. **Live isvalid response shape** — confirm the summary block shows REAL isvalid error messages (PascalCase casing assumption, RESEARCH A1) and that persist is not called on the invalid path.

(The third prior human-verification item — locale-safe numeric coercion — is now resolved in code via InvariantCulture and no longer requires a comma-decimal-locale host to verify.)

### Gaps Summary

No structural gaps. The gap-closure plan 04-03 fixed the UAT BLOCKER (Edit button disabled) at its root: `GetRoleDetailAsync` now unwraps the `Role` wrapper at a single defensive chokepoint, so `CanWrite` reads `__Permission` at the correct level and the Edit button enables for writable roles. The always-false CanCreate badge is removed. The CR-01 locale defect — previously the only standing correctness warning — is fixed with InvariantCulture. The build is clean and every plan grep gate passes.

The phase is `human_needed` rather than `passed` for one reason: three behavior-dependent truths assert runtime Turbo write-form behavior against a live authenticated API that no test exercises and that still cannot be smoke-tested due to the hashed-only host credential. This is the carried-forward STATE blocker A1/A4 — a verification-environment limitation, honestly recorded across all three plans, not an unfinished implementation. Live UAT Tests 1/2/3 are now UNBLOCKED (the disabled-button defect is gone) and become testable as soon as authenticated API credentials are available.

The EDIT-02 "inline" wording in REQUIREMENTS.md/ROADMAP SC#2 is consciously satisfied via a summary block per the user-approved D-07 decision — counted as intended scope, not a gap.

---

_Verified: 2026-06-24_
_Verifier: Claude (gsd-verifier)_
