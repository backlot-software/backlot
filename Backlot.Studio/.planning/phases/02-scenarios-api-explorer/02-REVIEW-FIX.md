---
phase: 02-scenarios-api-explorer
fixed_at: 2026-06-22T00:00:00Z
review_path: .planning/phases/02-scenarios-api-explorer/02-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 5
skipped: 0
status: all_fixed
---

# Phase 02: Code Review Fix Report

**Fixed at:** 2026-06-22
**Source review:** .planning/phases/02-scenarios-api-explorer/02-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 5 (CR-01, CR-02, WR-01, WR-02, WR-03)
- Fixed: 5
- Skipped: 0

## Fixed Issues

### CR-01: Stored XSS via endpoint path in `onclick` attribute

**Files modified:** `Backlot.Studio/Pages/Scenarios/Index.cshtml`, `Backlot.Studio/wwwroot/js/studio.js`
**Commit:** eef8a0f
**Applied fix:** Replaced `onclick="openScalarPanel('@s.Endpoints.FirstOrDefault()')"` with `data-endpoint="@s.Endpoints.FirstOrDefault()"` and `data-action="open-scalar"` on the button. Added an event delegation listener in `studio.js` that reads `btn.dataset.endpoint` (a plain string) and passes it to `openScalarPanel()`. Renamed the function parameter to `_endpointPath` to signal intentional non-use in v1.

---

### CR-02: `IndexModel.OnGetAsync` does not catch `BacklotApiUnauthorizedException`

**Files modified:** `Backlot.Studio/Pages/Scenarios/Index.cshtml.cs`, `Backlot.Studio/Pages/AuthenticatedPageModel.cs`
**Commit:** 5eb1e0f
**Applied fix:** `IndexModel` now inherits `AuthenticatedPageModel` (instead of `PageModel`) and wraps the API call in `SafeApiCall`. When the Backlot API returns 401, `SafeApiCall` catches `BacklotApiUnauthorizedException`, sets the `Turbo-Visit-Control: reload` header, and returns a redirect to `/Login` — matching the auth recovery pattern established in phase 01. The `HttpRequestException`/`TaskCanceledException` catch is preserved for API-unreachable errors (now with logging — see WR-03). This fix also covers WR-01 and WR-03 (committed in the same atomic commit).

---

### WR-01: `ViewData["Username"]` not set by `Scenarios/IndexModel`

**Files modified:** `Backlot.Studio/Pages/AuthenticatedPageModel.cs`, `Backlot.Studio/Pages/Scenarios/Index.cshtml.cs`
**Commit:** 5eb1e0f
**Applied fix:** Added `SetUserContext()` to `AuthenticatedPageModel`. The method reads `User.Identity?.Name` (the `ClaimTypes.Name` claim written at login) and stores it in `ViewData["Username"]`. `Scenarios/IndexModel.OnGetAsync` calls `SetUserContext()` at the start of the handler so the sidebar identity block is populated even when `/scenarios` is the first full-page render (e.g., accessed via a bookmarked `ReturnUrl`).

---

### WR-02: "Open API Docs" button active when Scalar CDN is blocked

**Files modified:** `Backlot.Studio/wwwroot/js/studio.js`
**Commit:** d22bd25
**Applied fix:** When `typeof Scalar === 'undefined'` (CDN blocked or not yet loaded), the initialization handler now sets `panel.dataset.scalarFailed = 'true'` before returning. `openScalarPanel()` guards on `panel.dataset.scalarFailed` and returns immediately, preventing a click from opening an empty white slide-in panel. The `panel.dataset.scalarFailed` guard was also added as part of the CR-01 commit (eef8a0f) where `openScalarPanel` was refactored.

---

### WR-03: Caught exception variable `ex` never logged

**Files modified:** `Backlot.Studio/Pages/Scenarios/Index.cshtml.cs`
**Commit:** 5eb1e0f
**Applied fix:** `ILogger<IndexModel>` is now injected via constructor. The `catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)` block calls `_logger.LogWarning(ex, "Failed to load scenarios from Backlot API")` before setting `ErrorMessage`, so connectivity failures are traceable in application logs.

---

_Fixed: 2026-06-22_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
