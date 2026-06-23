---
phase: 04-role-editing
verified: 2026-06-23T13:12:56Z
status: human_needed
score: 7/11 must-haves verified
behavior_unverified: 4
overrides_applied: 0
behavior_unverified_items:
  - truth: "Submitting the form with invalid data returns HTTP 422 and re-renders the form body; submitting valid data returns HTTP 303 to /roles/{uid}"
    test: "Against a running Backlot API with valid Basic Auth credentials: from a role detail page click Edit (Turbo Drive nav), submit clearly-invalid data and watch the network tab; then submit valid data."
    expected: "Invalid POST returns HTTP 422 with the form body re-rendered IN PLACE (Turbo swaps the body). Valid POST returns HTTP 303 with Location: /roles/{uid}?saved=1 and Turbo navigates to the detail page. No 400 antiforgery failure."
    why_human: "The 303/422 status codes are set correctly in TurboEditPageModel (verified by read), but whether Turbo Drive actually follows the 303 and swaps on 422 — surviving a prior Turbo navigation and the antiforgery round-trip — is a runtime client-server behavior. No test exercises it and the live smoke test could not run (host stores only a hashed password; no recoverable plaintext for Basic Auth)."
  - truth: "Submitting invalid data calls role/isvalid first, re-renders (422) with a summary block listing the validation errors, and does NOT call persist"
    test: "Submit invalid data and inspect the API access logs / observe no state change; confirm the summary block lists the REAL isvalid error messages returned by the live API."
    expected: "role/isvalid is called, returns IsValid=false with Results[]; persist/persist is NOT called; the summary block 'Please fix the following before saving:' lists each real ErrorMessage. The defensive empty-Results fallback ('Validation failed.') only fires if the API returns invalid with no Results."
    why_human: "The isvalid-before-persist ORDERING is statically correct (read-verified: persist is unreachable until IsValid passes), but the live isvalid response SHAPE/casing (PascalCase IsValid/Results/ErrorMessage — RESEARCH A1) was never confirmed against a real authenticated response. If the live casing differs, the summary block silently shows the generic fallback instead of the real errors. No test; could not smoke-test (credential limitation)."
  - truth: "Submitting valid data calls persist/persist and on success redirects (303) to /roles/{uid}, where a green 'Role saved.' banner appears"
    test: "Save a valid edit successfully and confirm you land on /roles/{uid}?saved=1 with the green dismissible 'Role saved.' banner; then visit /roles/{uid} directly (no flag) and confirm no banner."
    expected: "303 redirect to /roles/{uid}?saved=1; the detail page binds Saved=true from the query flag and renders the alert-success banner; the field values shown reflect the persisted change."
    why_human: "The ?saved=1 -> bool Saved -> banner wiring is statically verified, but that persist/persist actually stores the change AND the round-trip to the banner works end-to-end is runtime behavior. No test; could not smoke-test (credential limitation)."
  - truth: "The persist payload is built from the schema field list (type-coerced), skipping Calculated/read-only fields, so arbitrary fields cannot be injected"
    test: "On a host configured with a comma-decimal locale (de-DE, nl-NL, fr-FR), edit a role with a Decimal/Double/Single field, enter a value like 1.5, save, and inspect the persisted value."
    expected: "1.5 is persisted as 1.5. (Mass-assignment safety itself is read-verified: BuildPayload iterates Schema.Fields, never posted keys, and skips IsReadOnly.)"
    why_human: "The schema-driven mass-assignment guard is structurally correct and read-verified. BUT CoerceByType (04-REVIEW Critical) uses default-culture TryParse: on a non-invariant locale '1.5' parses as 15 or fails — silent data corruption persisted via persist/persist. This is environment-dependent (passes on en-US, corrupts on comma-decimal locales) so the type-coercion half of this truth is behavior-unverified and locale-dependent. No test; the locale path cannot be confirmed without running on such a host."
human_verification:
  - test: "Against a running Backlot API with valid Basic Auth credentials: from a role detail page click Edit (Turbo Drive nav), submit invalid data, then submit valid data; watch the network tab."
    expected: "Invalid -> HTTP 422 + form body re-rendered in place. Valid -> HTTP 303 + Location /roles/{uid}?saved=1 + Turbo navigates to detail. No 400 antiforgery error. (ROADMAP SC #3, carried-forward risk A4.)"
    why_human: "Runtime Turbo client-server behavior surviving a prior Turbo nav + antiforgery; no test; live smoke test blocked by hashed-only host password."
  - test: "Submit invalid data and confirm the summary block lists the REAL isvalid error messages (not the generic 'Validation failed.' fallback); confirm persist is not called."
    expected: "Real per-error ErrorMessage strings rendered in the top-of-form alert-danger summary; no persisted change."
    why_human: "Live isvalid response casing/shape (RESEARCH A1) never confirmed against an authenticated response; DTO is PascalCase on assumption. No test; credential limitation."
  - test: "On a comma-decimal-locale host (de-DE/nl-NL/fr-FR), edit a Decimal/Double/Single field with value 1.5 and verify the persisted value is 1.5, not 15."
    expected: "1.5 persists as 1.5."
    why_human: "04-REVIEW Critical: CoerceByType uses culture-sensitive TryParse — latent data-integrity defect that only manifests off en-US. Not exercised by any test."
---

# Phase 4: Role Editing Verification Report

**Phase Goal:** A user can edit any writable role through a schema-driven form, see inline validation feedback, and save changes — completing the final Core Value pillar (mutate) while isolating the Turbo form hazards in one place.
**Verified:** 2026-06-23T13:12:56Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

The phase's STATIC structure is fully and correctly implemented: every required artifact exists, is substantive, is wired, and the critical code paths (schema-driven payload build, isvalid-before-persist ordering, 303/422 status setting, ?saved=1 banner wiring) are correct by manual read. The build succeeds with 0 errors and every plan grep gate passes.

The phase does NOT reach `passed` because the goal hinges on **runtime Turbo write-form behavior** (303 redirect followed, 422 body swap, isvalid response casing, persist round-trip) that:
1. No automated test exercises (the Studio project has zero tests), and
2. Could not be live smoke-tested — the demo host (`Backlot.Demo.Web/usersandgroups.json`) stores only a salted password hash, so Basic Auth could not be satisfied to drive an authenticated browser session. Both executors recorded this honestly as carried-forward risk A1/A4.

These are behavior-dependent truths (state transitions / cancellation-of-persist invariant). Per the verifier's behavior-dependence rule, symbol presence + correct wiring is necessary but not sufficient — they are marked PRESENT_BEHAVIOR_UNVERIFIED and routed to human verification. This is a verification limitation, not an implementation gap.

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | User navigates to /roles/{uid}/edit and sees a form with all editable fields rendered from director/roles schema, pre-filled from seekbase/detail | ✓ VERIFIED | `Edit.cshtml` `@page "/roles/{uid}/edit"`; `OnGetAsync` fetches GetRoleDetailAsync + GetRoleSchemaAsync, MatchSchema by __Skills[0], seeds Fields from DetailModel.GetStringField (Edit.cshtml.cs:34-70); view loops Model.SchemaFields |
| 2 | Bool->checkbox, numeric->number input, everything else->text input | ✓ VERIFIED | `Edit.cshtml:57-84` three widget branches; IsBool/IsNumeric helpers (Edit.cshtml.cs:216-225); grep widget branches=4 |
| 3 | A Calculated field renders as a disabled display value, not an editable input | ✓ VERIFIED | `IsReadOnly` checks Characteristic=="Calculated" exactly (Edit.cshtml.cs:213-214); view renders disabled bg-light input + "Read-only" hint (Edit.cshtml:57-63); Required/StringLength/Range NOT treated as read-only |
| 4 | The Uid appears read-only at top with a copy button, carried in a hidden field | ✓ VERIFIED | `Edit.cshtml:20-26` copy row (data-action=copy-uid wired to wwwroot/js/studio.js:71) + "cannot be changed" hint; hidden `<input asp-for="Uid">` at line 52 |
| 5 | Submitting invalid -> HTTP 422 + re-render form body; valid -> HTTP 303 to /roles/{uid} | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | TurboInvalidPage sets 422 (TurboEditPageModel.cs:25-29), TurboRedirect sets 303 SeeOther + Location (cs:17-22). Status-code SETTING verified; Turbo client FOLLOW behavior + antiforgery round-trip never runtime-tested (credential limitation, no test) |
| 6 | Invalid data calls isvalid first, 422 + summary block, does NOT call persist | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Ordering read-verified: isvalid at cs:117, returns TurboInvalidPage at cs:130 BEFORE persist at cs:133 (persist unreachable when invalid). Summary block present (Edit.cshtml:28-39). Live isvalid casing/shape (A1) unconfirmed; no test |
| 7 | Valid data calls persist, 303 to /roles/{uid}, green "Role saved." banner appears | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | persist at cs:133, TurboRedirect to /roles/{Uid}?saved=1 at cs:136; Detail binds `bool Saved` (Detail.cshtml.cs:20-21) and renders alert-success banner (Detail.cshtml:20-26). End-to-end persist round-trip never runtime-tested |
| 8 | Success banner driven by ?saved=1 query flag read by detail page — no TempData | ✓ VERIFIED | `[BindProperty(SupportsGet=true)] public bool Saved` (Detail.cshtml.cs:20-21); banner gated on Model.Saved; grep TempData=0 in both edit and detail files |
| 9 | Persist payload built from schema field list, skipping Calculated, type-coerced (mass-assignment safe) | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Mass-assignment guard read-verified: BuildPayload iterates Schema.Fields (never posted keys), skips IsReadOnly, seeds only Uid + schema fields (cs:170-180). BUT CoerceByType uses culture-sensitive TryParse (04-REVIEW Critical) — type coercion silently corrupts decimals off en-US locales. Coercion half is locale-dependent + untested |
| 10 | Non-validation persist failure (500/network) re-renders form with entered values + error message | ✓ VERIFIED | catch (HttpRequestException or TaskCanceledException) sets ErrorMessage + returns TurboInvalidPage (cs:138-143); Fields stay bound so the form re-renders with entries; alert-danger renders ErrorMessage (Edit.cshtml:10-16) |
| 11 | EDIT-02 pre-save validation errors shown (D-07: summary block, not inline) | ✓ VERIFIED | Summary block "Please fix the following before saving:" lists each ErrorMessage (Edit.cshtml:28-39). D-07 is a user-approved conscious deviation from the "inline" wording (CONTEXT.md:29, DISCUSSION-LOG.md:105 "User's choice: Summary block only") — verification treats summary-block as intended scope |

**Score:** 7/11 truths verified (4 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Pages/TurboEditPageModel.cs` | 303/422 helpers, base class | ✓ VERIFIED | class TurboEditPageModel : AuthenticatedPageModel; TurboRedirect (SeeOther/303 + Location), TurboInvalidPage (UnprocessableEntity/422 + Page()) |
| `Pages/Roles/Edit.cshtml.cs` | EditModel : TurboEditPageModel, OnGet/OnPost | ✓ VERIFIED | OnGetAsync (schema+detail+seed), OnPostAsync (isvalid->422 / persist->303), BuildPayload, CoerceByType, MatchSchema, IsReadOnly/IsBool/IsNumeric |
| `Pages/Roles/Edit.cshtml` | Schema-driven Turbo form, antiforgery, widget loop, Uid row | ✓ VERIFIED | @page route, @Html.AntiForgeryToken(), hidden Uid, per-type widgets, Uid copy row, summary block, Nothing-to-edit empty state, 1 btn-primary; no data-turbo=false, no Html.Raw |
| `Services/BacklotApiClient.cs` | 3 new methods via envelope helpers | ✓ VERIFIED | GetRoleSchemaAsync (GET director/roles), ValidateRoleAsync (POST role/isvalid), PersistRoleAsync (POST persist/persist) — reuse GetEnvelopeAsync/PostEnvelopeAsync, no new HttpClient |
| `Services/IBacklotApiClient.cs` | 3 new signatures | ✓ VERIFIED | All three declared (lines 14-16) |
| `Models/Api/RoleSchema.cs` | RoleSchema/FieldSchema/CharacteristicSchema | ✓ VERIFIED | Three POCOs, collections default to [] |
| `Models/Api/ValidationOutcome.cs` | ValidationOutcome/ValidationResultItem | ✓ VERIFIED | PascalCase IsValid/Results/ErrorMessage/MemberNames |
| `Pages/Roles/Detail.cshtml(.cs)` | Saved query-flag + alert-success banner | ✓ VERIFIED | public bool Saved bind + dismissible alert-success "Role saved." gated on Model.Saved |

### Key Link Verification

| From | To | Via | Status |
|------|-----|-----|--------|
| Edit.cshtml.cs | BacklotApiClient.cs | OnGet calls GetRoleSchemaAsync+GetRoleDetailAsync; OnPost calls ValidateRoleAsync+PersistRoleAsync | ✓ WIRED (cs:46,80,87,117,133) |
| Edit.cshtml.cs | TurboEditPageModel.cs | EditModel inherits; OnPost returns TurboRedirect/TurboInvalidPage | ✓ WIRED (grep=6 occurrences) |
| Edit.cshtml.cs | Detail.cshtml.cs | reuses DetailModel.GetStringField/GetSkills/GetPermissions/GetPageTitle | ✓ WIRED (cs:52,53,59,150) |
| Edit.cshtml.cs | Detail.cshtml.cs | TurboRedirect /roles/{uid}?saved=1 sets the flag Detail reads | ✓ WIRED (saved=1 at cs:136 -> bool Saved at Detail.cshtml.cs:21) |
| Edit.cshtml | Edit.cshtml.cs | view renders Model.ValidationErrors | ✓ WIRED (Edit.cshtml:28-39) |
| Edit.cshtml | wwwroot/js/studio.js | data-action=copy-uid event delegation | ✓ WIRED (studio.js:71) |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| Edit.cshtml | Model.SchemaFields | GetRoleSchemaAsync -> GET api/role/director/roles (real envelope) | Yes (live API) | ✓ FLOWING |
| Edit.cshtml | Fields (pre-fill) | GetRoleDetailAsync -> POST seekbase/detail, seeded via GetStringField | Yes (live API) | ✓ FLOWING |
| Edit.cshtml | Model.ValidationErrors | ValidateRoleAsync -> POST role/isvalid Body | Live API (casing unconfirmed) | ⚠️ STATIC fallback path exists; real-data path runtime-unverified |
| Detail.cshtml | Model.Saved | ?saved=1 query flag bound on GET | Yes (query bind) | ✓ FLOWING |

No hollow/disconnected props found. All field data originates from live API calls (Known Stubs: None, confirmed).

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Studio builds | `dotnet build Backlot.Studio/Backlot.Studio.csproj` | Build succeeded, 0 Error(s) | ✓ PASS |
| Edit endpoints exist + wired | (04-01 SUMMARY) GET director/roles->401, POST seekbase/detail->401, POST isvalid->500, POST persist->500 | All route into framework (auth-gated / reached scenario exec) | ✓ PASS (endpoints present) |
| Authenticated edit round-trip (303/422) | (would require valid Basic Auth) | Host stores hashed password only; no plaintext | ? SKIP — routed to human verification |

### Probe Execution

No probes declared in PLAN/SUMMARY and no `scripts/*/tests/probe-*.sh` present (Razor Pages UI phase). Step 7c not applicable.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| EDIT-01 | 04-01 | Form with all editable fields dynamically rendered from director/roles schema | ✓ SATISFIED | Truths 1-4; schema-driven widget loop, pre-fill from detail |
| EDIT-02 | 04-02 | Pre-save validation errors from role/isvalid shown (literal: "inline next to fields") | ✓ SATISFIED (D-07 deviation) | Truth 11; summary block. REQUIREMENTS.md/ROADMAP SC#2 say "inline" but D-07 is a recorded, user-approved deviation (CONTEXT.md:29, DISCUSSION-LOG.md:105). Treated as intended scope per the documented decision |
| EDIT-03 | 04-02 | Save via persist/persist; success->redirect to detail; failure->errors re-displayed | ✓ SATISFIED (structure) / behavior-unverified (runtime) | Truths 7,10; 303 redirect + banner + persist-failure handling all wired. Live persist round-trip routed to human verification |

All three declared requirement IDs (EDIT-01, EDIT-02, EDIT-03) are accounted for and map to Phase 4 in REQUIREMENTS.md traceability. No orphaned requirements — REQUIREMENTS.md maps exactly EDIT-01/02/03 to Phase 4, all claimed by plans.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Edit.cshtml.cs | 194-208 | `CoerceByType` default-culture TryParse | ⚠️ Warning | 04-REVIEW Critical: silent decimal/float corruption on comma-decimal locales. Affects Truth 9 type-coercion. No follow-up reference on the lines — but tracked in 04-REVIEW.md |
| (none) | — | TODO/FIXME/XXX/PLACEHOLDER debt markers | — | grep across all 8 phase files: 0 unreferenced debt markers found |
| (none) | — | Stub returns / hardcoded empty render data | — | All UI data flows from live API; Known Stubs: None (confirmed) |

No BLOCKER-class debt markers. The culture-parse defect is a real correctness warning (carried in 04-REVIEW) but is not a debt marker and does not block the phase goal on the dev locale.

### Human Verification Required

Three items (see frontmatter `human_verification`), all stemming from the same root limitation — the live authenticated Turbo round-trip could not be exercised because the demo host stores only a hashed password:

1. **Turbo 303/422 round-trip** — submit invalid (expect 422 + in-place body swap) and valid (expect 303 -> /roles/{uid}?saved=1 + Turbo nav) against a running API with valid Basic Auth; confirm no 400 antiforgery failure.
2. **Live isvalid response shape** — confirm the summary block shows REAL isvalid error messages (PascalCase casing assumption, RESEARCH A1) and that persist is not called on the invalid path.
3. **Locale-safe numeric coercion** — on a comma-decimal-locale host, save a Decimal/Double/Single field with value 1.5 and confirm it persists as 1.5 (04-REVIEW Critical).

### Gaps Summary

No structural gaps. Every artifact exists, is substantive, is wired, and carries real data flow; the build is clean and all plan grep gates pass. The save orchestration ordering (isvalid before persist, persist skipped on invalid), the 303/422 status setting, the mass-assignment-safe schema-driven payload, the TempData-free ?saved=1 banner, and the persist-failure value-preservation are all correct by read.

The phase is `human_needed` rather than `passed` for exactly one reason: four behavior-dependent truths assert runtime Turbo write-form behavior that no test exercises and that could not be smoke-tested due to the hashed-only host credential. This is a verification limitation honestly recorded by both executors, not evidence of an unfinished implementation. The single noted correctness risk (culture-sensitive numeric parse, 04-REVIEW Critical) is locale-dependent and folds into the same human-verification batch.

The EDIT-02 "inline" wording in REQUIREMENTS.md/ROADMAP SC#2 is consciously and explicitly satisfied via a summary block per the user-approved D-07 decision — counted as intended scope, not a gap.

---

_Verified: 2026-06-23T13:12:56Z_
_Verifier: Claude (gsd-verifier)_
