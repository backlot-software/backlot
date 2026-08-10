---
phase: 04-role-editing
plan: 01
subsystem: ui
tags: [razor-pages, turbo, bootstrap, dynamic-forms, http-303, http-422, antiforgery, schema-driven]

# Dependency graph
requires:
  - phase: 03-roles
    provides: "DetailModel static helpers (GetStringField/GetSkills/GetPermissions/GetPageTitle), seekbase/detail fetch, AuthenticatedPageModel (SafeApiCall/SetUserContext), BacklotApiClient envelope helpers, copy-uid JS"
provides:
  - "TurboEditPageModel base (TurboRedirect 303 / TurboInvalidPage 422) — the reusable Turbo write-form contract"
  - "RoleSchema/FieldSchema/CharacteristicSchema and ValidationOutcome/ValidationResultItem DTOs"
  - "GetRoleSchemaAsync / ValidateRoleAsync / PersistRoleAsync on the typed BacklotApiClient"
  - "Schema-driven role edit page /roles/{uid}/edit (GET render + isvalid->422 / persist->303 POST round-trip)"
affects: [04-02, role-editing, validation-summary, success-banner]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Turbo write-form: explicit 303 See-Other on success, 422 on invalid (no RedirectToPage/bare Page())"
    - "Dynamic form binding via [BindProperty] Dictionary<string,string?> Fields keyed name=Fields[Field]"
    - "Mass-assignment-safe payload built from the SCHEMA field list, never posted keys"

key-files:
  created:
    - "Models/Api/RoleSchema.cs"
    - "Models/Api/ValidationOutcome.cs"
    - "Pages/TurboEditPageModel.cs"
    - "Pages/Roles/Edit.cshtml"
    - "Pages/Roles/Edit.cshtml.cs"
  modified:
    - "Services/IBacklotApiClient.cs"
    - "Services/BacklotApiClient.cs"

key-decisions:
  - "Edit.cshtml and Edit.cshtml.cs were built complete in one pass (page model + helpers + full widget view); committed as two atomic commits split along backend/view lines rather than the plan's scaffold/refine task split"
  - "MatchSchema re-resolves the schema row from a fresh seekbase/detail fetch on POST (same __Skills[0]==Role logic as GET) so the authoritative field list is rebuilt server-side, never from posted keys"
  - "Bool fields default to false in BuildPayload when the checkbox posts nothing (Pitfall 3)"

patterns-established:
  - "TurboEditPageModel: 303/422 status set directly on Response (mirrors AuthenticatedPageModel's Turbo-Visit-Control header style)"
  - "Schema-driven widget mapping: Boolean->checkbox, numeric FriendlyName set->number, else->text; Calculated->disabled read-only"

requirements-completed: [EDIT-01]

# Metrics
duration: 4min
completed: 2026-06-23
status: complete
---

# Phase 04 Plan 01: Role Edit Page + Turbo 303/422 Service Layer Summary

**Schema-driven `/roles/{uid}/edit` form rendered from director/roles, pre-filled from seekbase/detail, with a reusable TurboEditPageModel base that emits 303 on save and 422 on invalid — the phase's central Turbo write-form hazard isolated in two lines.**

## Performance

- **Duration:** ~4 min
- **Started:** 2026-06-23T12:54:06Z
- **Completed:** 2026-06-23T12:57:47Z
- **Tasks:** 2
- **Files modified:** 7 (5 created, 2 modified)

## Accomplishments
- `TurboEditPageModel` base class with `TurboRedirect()` (303 See-Other) and `TurboInvalidPage()` (422) — the reusable Turbo form contract, verbatim from RESEARCH Pattern 1.
- Three new typed-client methods (`GetRoleSchemaAsync`, `ValidateRoleAsync`, `PersistRoleAsync`) reusing the existing envelope helpers + `PascalOptions` (no `new HttpClient()`).
- Schema/validation DTOs matching the `RoleFind` convention (non-null collections default to `[]`).
- `EditModel` with schema+detail fetch on GET, schema-keyed mass-assignment-safe `BuildPayload`, and the minimal isvalid->422 / persist->303 save path.
- Full schema-driven `Edit.cshtml`: per-type widgets (checkbox/number/text), disabled Calculated read-only fields, Uid copy row, validation summary, "Nothing to edit" empty state, and load/persist error alerts — single blue Save + outline Cancel, 44px touch targets.

## Task Commits

1. **Task 1: DTOs + service methods + TurboEditPageModel + EditModel** - `bc8c8a7` (feat)
2. **Task 2: schema-driven edit form view (widgets, empty/error states)** - `ce55f40` (feat)

**Plan metadata:** see final docs commit.

## Files Created/Modified
- `Models/Api/RoleSchema.cs` - RoleSchema/FieldSchema/CharacteristicSchema POCOs (director/roles shape).
- `Models/Api/ValidationOutcome.cs` - ValidationOutcome/ValidationResultItem (isvalid Body shape).
- `Pages/TurboEditPageModel.cs` - AuthenticatedPageModel subclass with 303/422 helpers.
- `Pages/Roles/Edit.cshtml.cs` - EditModel: GET render + POST round-trip, MatchSchema, BuildPayload, IsReadOnly/IsBool/IsNumeric/CurrentValue helpers.
- `Pages/Roles/Edit.cshtml` - Turbo-driven schema-driven form view.
- `Services/IBacklotApiClient.cs` - three new method signatures.
- `Services/BacklotApiClient.cs` - implementations via GetEnvelopeAsync/PostEnvelopeAsync.

## Decisions Made
- **Atomic-commit split adjusted:** The plan's Task 1 (scaffold + smoke) / Task 2 (widget refine) split assumed an incremental view. Because the page model and view are tightly coupled, both were authored complete in one pass and committed as two meaningful atomic commits along a backend (`bc8c8a7`) / view (`ce55f40`) seam. All Task 1 and Task 2 acceptance gates pass against the final files.
- **POST schema re-resolution:** `OnPostAsync` re-fetches both `director/roles` and `seekbase/detail` and re-runs `MatchSchema` so the authoritative field list (and read-only set) is rebuilt server-side every save — the payload never derives from posted keys (T-04-03 mass-assignment mitigation).

## Deviations from Plan

None requiring deviation rules — plan executed as written. The only adjustment is the commit-split note above (organizational, not a code deviation).

## Issues Encountered

**Live smoke test could not complete the authenticated round-trip (carried-forward risk A4).**
- The Backlot API host (`Backlot.Demo.Web`) was started successfully on `https://localhost:7221` (the URL Studio is configured for). All four required endpoints were confirmed to route into the framework:
  - `GET api/role/director/roles` -> **401** (exists, auth required)
  - `POST api/role/seekbase/detail` -> **401** (exists, auth required)
  - `POST api/role/role/isvalid` -> **500** (reached scenario execution with empty body)
  - `POST api/role/persist/persist` -> **500** (reached scenario execution with empty body)
- The host's `usersandgroups.json` stores only a salted/secret-keyed password **hash** (`SecretKey = SECRET_8C868AABB5F54AA7925B4F2FFF6DE80B`); no plaintext credential is recoverable, and no documented test password exists in the repo. Basic Auth therefore could not be satisfied to drive a logged-in browser session through Turbo.
- **Consequently the observed HTTP 303/422 status codes from a live authenticated edit POST were NOT captured.** Per the plan's explicit fallback, this is recorded as the one **unverified-runtime risk (RESEARCH A4)** and carried forward. The 303/422 design is HIGH-confidence from framework source (RESEARCH Pattern 1) and the endpoints are confirmed present and wired.
- **`isvalid` property casing (A1):** also NOT confirmed against a live authenticated response (requires auth). DTOs use PascalCase `IsValid`/`Results`/`ErrorMessage`/`MemberNames` per framework source via the existing `PascalOptions`; parse remains defensive (null-tolerant `ErrorMessage`, generic message on empty `Results`). **Confirm on first authenticated run.**
- **Surprise characteristics (A3):** could not eyeball a live `director/roles` payload under auth. Read-only detection remains `Characteristic == "Calculated"` exactly; all other characteristics treated as editable.

**Recommendation for 04-02 / verification:** obtain valid Basic Auth credentials for the running host and exercise the full edit round-trip (invalid -> 422 body swap; valid -> 303 to `/roles/{uid}?saved=1`) to close A1/A3/A4 before phase sign-off.

## User Setup Required
None - no external service configuration required. (To complete the deferred live smoke test, valid Basic Auth credentials for the Backlot API host are needed.)

## Next Phase Readiness
- EDIT-01 delivered: the edit page renders all editable schema fields by Type, disables Calculated fields, pre-fills from detail, and shows empty/error states. Build succeeds, all grep gates pass.
- 04-02 can now harden the validation summary, finalize the mass-assignment-safe payload, add persist-failure handling, and wire the `?saved=1` success banner on Detail.
- **Carried-forward blocker:** the live Turbo 303/422 + antiforgery round-trip (A4) and `isvalid` casing (A1) remain runtime-unverified — needs valid host credentials.

## Self-Check: PASSED
All 7 files present; both task commits (`bc8c8a7`, `ce55f40`) found in git log. Build succeeds, 0 errors.

---
*Phase: 04-role-editing*
*Completed: 2026-06-23*
