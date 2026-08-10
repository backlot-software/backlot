---
phase: quick-260708-lg9
plan: 01
subsystem: Backlot.Studio / Roles
status: complete
tags: [studio, roles, http-download, turbo, security]
requires:
  - Backlot.Studio/Pages/Roles/Detail.cshtml.cs
  - Backlot.Studio/Services/IBacklotApiClient.cs
provides:
  - "OnGetDownloadAsync handler emitting a .http request template for POST /api/role/{RoleType}/persist"
  - "Download HTTP request action on the role detail page (Turbo-disabled real download)"
affects:
  - Backlot.Studio/Pages/Roles/Detail.cshtml
  - Backlot.Studio/Pages/Roles/Detail.cshtml.cs
tech-stack:
  added: []
  patterns:
    - "System.Text.Json.Nodes.JsonObject for insertion-ordered, throw-safe JSON body serialization"
    - "File(byte[], contentType, fileDownloadName) overload for browser attachment download"
    - "data-turbo=\"false\" to opt an anchor out of Turbo Drive for a real file download"
key-files:
  created: []
  modified:
    - Backlot.Studio/Pages/Roles/Detail.cshtml.cs
    - Backlot.Studio/Pages/Roles/Detail.cshtml
  deleted:
    - Backlot.Studio/Pages/Roles/Edit.cshtml
    - Backlot.Studio/Pages/Roles/Edit.cshtml.cs
decisions:
  - "Reuse IBacklotApiClient.BaseUrl (trailing slash trimmed) for @baseUrl instead of injecting IConfiguration — no new constructor dependency"
  - "Authorization line is a static placeholder only; session credentials are never read/embedded (T-lg9-01)"
  - "Download handler re-fetches the role (handler invocations do not share GET-populated RoleData)"
metrics:
  duration: ~10 min
  completed: 2026-07-08
  tasks: 2
  files: 4
---

# Phase quick-260708-lg9 Plan 01: Remove Edit Roles, add Download HTTP request Summary

Replaced the in-app role-editing surface in Backlot.Studio with a "Download HTTP request" action that serves a ready-to-edit `.http` template (`POST {{baseUrl}}/api/role/{RoleType}/persist`) pre-filled with the role's current fields (Uid first) and a credential placeholder — credentials are never embedded.

## What Changed

### Task 1 — Delete Edit page, add .http download handler (commit `17f711b`)
- Deleted `Pages/Roles/Edit.cshtml` and `Edit.cshtml.cs` — the sole carriers of `EditModel`, the `/roles/{roletype}/{uid}/edit` route, and the `?saved=1` redirect. Grep confirmed no other references.
- Removed the now-dead `Saved` bind property (and its comment) from `DetailModel`.
- Added `OnGetDownloadAsync` (reachable via `?handler=Download` on the existing detail route). It calls `SetUserContext()`, returns `NotFound()` on blank `Uid`, re-fetches the role via `SafeApiCall(... _api.PlayAsync<JsonElement>("seekbase", "detail", new { For = Uid }))`, unwraps `Role`, and returns a `text/plain` attachment named `{RoleType}-{Uid}.http`. The fetch is wrapped in the same `HttpRequestException`/`TaskCanceledException` catch as `OnGetAsync`; it logs a warning and returns `NotFound()` on failure — never throws (T-lg9-02).
- Added helpers: `BuildHttpRequest` (`@baseUrl` var with trailing slash trimmed, blank line, `POST {{baseUrl}}/api/role/{RoleType}/persist`, `Content-Type`, an explanatory `#` comment, placeholder `Authorization: Basic <base64 of username:password>`, blank line, JSON body), `BuildBody` (iterates `GetNonSystemFields`, Uid-first, into a `JsonObject`, pretty-printed), and `BuildFileName` (invalid path chars replaced with `_`).

### Task 2 — Replace Edit button with Download action (commit `d7f0da7`)
- Replaced the entire `@if (Model.CanWrite) { ... } else { ... }` Edit-button block with a single unconditional `<a href="/roles/@Model.RoleType/@Model.Uid?handler=Download" data-turbo="false" class="btn btn-primary">` containing `<i class="bi bi-download"></i>` + `<span>Download HTTP request</span>`. Not gated on write permission.
- Removed the dead `@if (Model.Saved)` success banner. The error banner and everything below are unchanged.

## Deviations from Plan

None — plan executed exactly as written.

Note on task-commit ordering: Task 1's `<verify>` includes a full `dotnet build`, which necessarily fails in isolation because `Detail.cshtml` still referenced `Model.Saved` (removed only in Task 2). This is an inherent coupling in the plan, not a deviation — Task 1's C# compiles correctly, and the full solution builds clean after Task 2. Verified below.

## Threat Mitigations Applied
- **T-lg9-01 (Information Disclosure):** The `.http` Authorization line is a hardcoded placeholder; no session credential is read or serialized. An explanatory `#` comment instructs the user to supply their own base64 `username:password`.
- **T-lg9-02 (Robustness/DoS):** The API re-fetch is wrapped in the existing exception catch returning `NotFound()`; field values are serialized as strings via `JsonObject`, so malformed data never throws.

## Verification
- `dotnet build Backlot.Studio/Backlot.Studio.csproj` — **0 errors**, 2 pre-existing warnings (CS8618 in `Models/Api/Status.cs`, CS8603 in `Detail.cshtml.cs` `GetStringField`, both predate this change; out of scope).
- `Edit.cshtml` and `Edit.cshtml.cs` no longer exist.
- `Detail.cshtml.cs` contains `OnGetDownloadAsync`; no `public bool Saved` remains.
- `Detail.cshtml` contains `handler=Download`, `data-turbo="false"`, `Download HTTP request`; no `Model.Saved` or `/edit` reference remains.

## Known Stubs
None.

## Self-Check: PASSED
- FOUND: Backlot.Studio/Pages/Roles/Detail.cshtml.cs (OnGetDownloadAsync present)
- FOUND: Backlot.Studio/Pages/Roles/Detail.cshtml (Download action present)
- CONFIRMED DELETED: Backlot.Studio/Pages/Roles/Edit.cshtml, Edit.cshtml.cs
- FOUND commit: 17f711b (Task 1)
- FOUND commit: d7f0da7 (Task 2)
