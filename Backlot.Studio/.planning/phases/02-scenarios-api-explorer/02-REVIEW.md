---
phase: 02-scenarios-api-explorer
reviewed: 2026-06-22T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - Models/Api/ScenarioItem.cs
  - Pages/Scenarios/Index.cshtml.cs
  - Pages/Scenarios/Index.cshtml
  - Services/IBacklotApiClient.cs
  - Services/BacklotApiClient.cs
  - Pages/Shared/_Sidebar.cshtml
  - Pages/Shared/_Layout.cshtml
  - wwwroot/js/studio.js
  - wwwroot/css/studio.css
findings:
  critical: 2
  warning: 3
  info: 2
  total: 7
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-06-22
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

This phase adds the Scenarios index page (`/scenarios`): a grouped, card-based listing of all registered Backlot scenarios with an "Open API Docs" button that slides in the Scalar reference panel. It also wires up `GetScenariosAsync` on the API client, initializes the Scalar panel in `studio.js`, and adds the corresponding CSS slide-in animation.

The implementation is clean at a structural level. Two blockers exist. The first is a stored XSS vector: endpoint path strings returned by the API are interpolated into an inline `onclick` attribute using only Razor's HTML encoder. HTML encoding is not sufficient to prevent script injection in event-handler attribute context because the browser decodes HTML entities before passing the attribute value to the JS engine. The second blocker is a missing catch for `BacklotApiUnauthorizedException` in `IndexModel`: the catch clause only covers `HttpRequestException` and `TaskCanceledException`, so a session expiry during the scenarios fetch causes an unhandled 500 error page instead of a redirect to login — breaking the established auth recovery pattern.

Three warnings cover: a username display gap in the sidebar when `/scenarios` is reached via `ReturnUrl` redirect, a silently-suppressed Scalar panel when the CDN is blocked (the button remains active but does nothing), and the exception variable `ex` being captured but never logged.

Note: two blockers carried forward from the phase 01 review (`SafeApiCall` redirect being overwritten by `Page()`, and session credential leak on network error in `Login.cshtml.cs`) are not re-listed here as they belong to files outside this phase's scope, but they remain open.

---

## Critical Issues

### CR-01: Stored XSS via endpoint path in `onclick` attribute — HTML encoding is insufficient in event-handler context

**File:** `Pages/Scenarios/Index.cshtml:49`

**Issue:** The "Open API Docs" button uses an inline event handler that embeds an API-controlled string directly into JavaScript:

```cshtml
onclick="openScalarPanel('@s.Endpoints.FirstOrDefault()')"
```

Razor's `@` expression applies `HtmlEncoder`, which converts `'` to `&#x27;`, `<` to `&lt;`, etc. This prevents HTML tag injection but does **not** prevent JavaScript injection in an `onclick` attribute. The browser decodes HTML entities inside attribute values before the JS engine parses the handler text. A scenario endpoint path containing `'); alert(document.cookie); //` would be encoded to `&#x27;); alert(document.cookie); //` in the HTML source, but the browser would decode it back to `'); alert(document.cookie); //` before evaluating the handler — executing the injected code.

The endpoint data originates from the Backlot API. An attacker with the ability to register a scenario with a crafted endpoint path (e.g., through the API directly) would achieve stored XSS execution in every Studio user's browser session whenever the Scenarios page is loaded.

**Fix:** Remove the inline `onclick` attribute. Store the endpoint path in a `data-*` attribute (which Razor HTML-encodes correctly for attribute values and which the browser does not treat as executable), then read it in JavaScript:

```cshtml
{{!-- Index.cshtml: replace onclick with data attribute --}}
<button class="btn btn-primary btn-sm"
        style="min-height:44px"
        data-endpoint="@s.Endpoints.FirstOrDefault()"
        data-action="open-scalar">
    Open API Docs
</button>
```

```javascript
// studio.js: delegate click handling via event delegation
document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-action="open-scalar"]');
    if (!btn) return;
    openScalarPanel(btn.dataset.endpoint ?? '');
});
```

`data-*` attributes are HTML-attribute-encoded by Razor's `@`, and `dataset.endpoint` reads the decoded value as a plain string — no JS injection path exists.

---

### CR-02: `IndexModel.OnGetAsync` does not catch `BacklotApiUnauthorizedException` — session expiry causes unhandled 500

**File:** `Pages/Scenarios/Index.cshtml.cs:32-35`

**Issue:** `IndexModel` extends `PageModel` directly (not `AuthenticatedPageModel`) and its `OnGetAsync` has a catch clause that covers only `HttpRequestException` and `TaskCanceledException`:

```csharp
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
```

`BasicAuthHandler.SendAsync` throws `BacklotApiUnauthorizedException` (which extends `Exception`, not `HttpRequestException`) when the Backlot API returns HTTP 401. This happens when the session's `BasicAuthHeader` key has expired or been cleared mid-session. The exception bypasses the catch clause, propagates as an unhandled exception, and renders the generic error page rather than redirecting to `/login`.

This violates the auth recovery contract established in phase 01 (`AuthenticatedPageModel.SafeApiCall` + `Turbo-Visit-Control: reload` pattern) and means any session timeout on the Scenarios page results in a confusing crash.

**Fix:** Either (a) extend `AuthenticatedPageModel` and use `SafeApiCall`, or (b) add `BacklotApiUnauthorizedException` to the catch predicate and issue the redirect manually:

```csharp
// Option A — preferred: inherit AuthenticatedPageModel and use SafeApiCall
public class IndexModel : AuthenticatedPageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var result = await SafeApiCall(async () => await _api.GetScenariosAsync());
        if (Response.HasStarted) return new EmptyResult(); // redirect was issued by SafeApiCall
        Groups = (result ?? [])
            .GroupBy(s => s.Tags.Length > 0 ? s.Tags[0] : "Uncategorized")
            .Select(g => (g.Key, g.AsEnumerable()))
            .ToList();
        return Page();
    }
}
```

Note: Option A still depends on the phase 01 CR-01 fix (SafeApiCall redirect overwrite) being applied first.

---

## Warnings

### WR-01: `ViewData["Username"]` is not set by `Scenarios/IndexModel` — sidebar shows blank username when `/scenarios` is the first authenticated page loaded

**File:** `Pages/Scenarios/Index.cshtml.cs` (no line — omission), `Pages/Shared/_Sidebar.cshtml:28`

**Issue:** `_Sidebar.cshtml` renders `@ViewData["Username"]` to show the logged-in user. `ViewData["Username"]` is only populated in `Pages/Index.cshtml.cs` (the dashboard page). `Pages/Scenarios/Index.cshtml.cs` never sets it.

For the normal navigation flow (login → dashboard → scenarios via Turbo Drive), the sidebar is `data-turbo-permanent` so its DOM is preserved from the dashboard render and shows the correct username. However, if the user is redirected directly to `/scenarios` as a `ReturnUrl` after login (e.g., they bookmarked `/scenarios` and hit the login wall), the first full-page render is the scenarios page, and `ViewData["Username"]` is null — the sidebar shows a blank identity block.

**Fix:** All `PageModel` classes that render within `_Layout.cshtml` should set `ViewData["Username"]`. Centralise this in `AuthenticatedPageModel` so it cannot be missed by future pages:

```csharp
// AuthenticatedPageModel.cs — add to OnPageHandlerExecutionAsync or a base OnGet helper
protected void SetUserContext()
{
    ViewData["Username"] = User.Identity?.Name ?? "Unknown user";
}
```

`User.Identity.Name` is populated from the cookie claim `ClaimTypes.Name` set in `Login.cshtml.cs:52` — no extra API call is needed.

---

### WR-02: Scalar panel "Open API Docs" button is active even when Scalar failed to initialize — silent no-op on CDN block

**File:** `wwwroot/js/studio.js:33`, `Pages/Scenarios/Index.cshtml:47-50`

**Issue:** `studio.js` guards Scalar initialization with:

```javascript
if (typeof Scalar === 'undefined') return;
```

If the CDN script is blocked (firewall, CSP violation, network error), `Scalar` is never defined and `panel.dataset.scalarInitialized` is never set. The "Open API Docs" button remains visible and fully styled. Clicking it calls `openScalarPanel()`, which adds `is-open` to the panel and shows the backdrop — but the panel is empty (`#scalar-mount` has no content). The user sees a blank white slide-in panel with no explanation.

**Fix:** Track whether initialization succeeded and disable/hide the button when it did not:

```javascript
// After the typeof check, set a failure flag
if (typeof Scalar === 'undefined') {
    panel.dataset.scalarFailed = 'true';
    return;
}
// ... init succeeds ...
panel.dataset.scalarInitialized = 'true';
```

```javascript
// In openScalarPanel():
function openScalarPanel(endpointPath) {
    const panel = document.getElementById('scalar-panel');
    if (!panel || panel.dataset.scalarFailed) return; // silently ignore — or show a toast
    // ...
}
```

Alternatively, render the button conditionally once Scalar is confirmed loaded, or add visible fallback content inside `#scalar-mount` for the failure case.

---

### WR-03: Caught exception variable `ex` is declared but never logged or inspected

**File:** `Pages/Scenarios/Index.cshtml.cs:32`

**Issue:**

```csharp
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
```

`ex` is bound but never used — there is no logging statement in the catch block. When the API is unreachable, the failure reason is silently discarded. This makes diagnosing connectivity problems harder: there is no way to distinguish a DNS failure from a TLS error from a timeout in the application logs.

**Fix:** Inject `ILogger<IndexModel>` and log the exception at warning level before setting `ErrorMessage`:

```csharp
public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
{
    _api = api;
    _logger = logger;
}

catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
{
    _logger.LogWarning(ex, "Failed to load scenarios from Backlot API");
    ErrorMessage = "Could not load scenarios. Check that the Backlot API is reachable and that your credentials are valid.";
}
```

---

## Info

### IN-01: `openScalarPanel` parameter `endpointPath` is accepted but never used

**File:** `wwwroot/js/studio.js:51`

**Issue:**

```javascript
function openScalarPanel(endpointPath) {
    // v1: open at top level; hash deep-linking deferred
}
```

The parameter is named and documented as a v1 deferral, which is acceptable. However, because the parameter is silently dropped, callers passing a non-empty string receive no acknowledgment or error — and the comment about hash deep-linking being "Scalar-version-internal" may not remain accurate across Scalar upgrades. If this parameter is intentionally dead for v1, use `_endpointPath` or add a `/* eslint-disable-next-line no-unused-vars */` annotation to communicate intent clearly.

**Fix:** Rename to signal intentional non-use:

```javascript
function openScalarPanel(_endpointPath) {
    // Deep-link to endpoint deferred to v2; Scalar hash format is internal to its version
```

---

### IN-02: `ScenarioItem.Scenario` and `ScenarioItem.Result` are declared `null!` but rendered without null guard in the template

**File:** `Models/Api/ScenarioItem.cs:5-6`, `Pages/Scenarios/Index.cshtml:36-37`

**Issue:** Both properties use the `null!` suppressor, signalling that deserialization is expected to always populate them. If the API returns a scenario object with a missing or explicitly-null `Scenario` or `Result` field (malformed response, API schema change), both properties will be `null` at runtime. Razor's `@s.Scenario` and `@s.Result` render `null` as an empty string without crashing, but the resulting card shows blank name and return type fields with no indication to the user that the data is incomplete.

**Fix:** Either initialise to `string.Empty` (removes the nullability suppressor, reflects intent that these are always strings):

```csharp
public string Scenario { get; set; } = string.Empty;
public string Result { get; set; } = string.Empty;
```

Or keep `null!` but guard in the template:

```cshtml
<h5 class="mb-1 fw-semibold">@(s.Scenario ?? "(unnamed)")</h5>
<small class="text-muted">Returns: @(s.Result ?? "unknown")</small>
```

---

_Reviewed: 2026-06-22_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
