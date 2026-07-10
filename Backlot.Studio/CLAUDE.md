# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

Backlot.Studio is a standalone .NET 10 Razor Pages web application — a management frontend for the Backlot API. It lets developers and operators browse scenarios, search/view/edit persisted roles, and inspect role relations. It is a **thin presentation layer with no database of its own**: every page fetches data from the Backlot API at request time.

This is an experimental "vibe-programmed" interface, not a production-hardened admin panel.

## Commands

```bash
# Run in development (with Razor runtime compilation)
dotnet run

# Build
dotnet build

# Publish
dotnet publish

# Store API base URL outside source (development)
dotnet user-secrets set "BacklotApi:BaseUrl" "https://localhost:7221"
```

There are no automated tests. The Backlot API must be running separately (default: `https://localhost:7221`).

## Architecture

### System Boundary

The browser never sees credentials. The flow is:

```
Browser (Turbo Drive/Frames)
  ↕ cookie (.Studio.Session, HTTP-only)
Razor Pages app (this project)
  └─ BasicAuthHandler injects Basic Auth from session
  ↕ HTTPS + Authorization: Basic <base64>
Backlot API (separate process, default https://localhost:7221)
```

### Key Patterns

**API calls:** All HTTP goes through `IBacklotApiClient` → `BacklotApiClient`. PageModels never instantiate `HttpClient` or touch headers. The underlying API convention is `api/role/{roleName}/{scenario}` (GET or POST), which maps to `PlayAsync<T>()`.

**Envelope unwrapping:** Every Backlot API response is `{ "Body": …, "Status": …, "TimeInMs": … }`, modelled as `ApiEnvelope<T>`. Role detail bodies are dynamic (`JsonElement`) because role schemas are open-ended. System fields on roles use the `__` prefix (e.g., `__Permission`, `__Skills`).

**Credentials:** On login, `username:password` is base64-encoded and stored in server-side session under the key `"BasicAuthHeader"` (without the `"Basic "` prefix). `BasicAuthHandler` reads this from `IHttpContextAccessor` inside `SendAsync` on every outbound request — credentials are **never cached in a field** (IHttpClientFactory reuses handler instances across users).

**Auth flow:** Cookie authentication gates all pages via a fallback `RequireAuthenticatedUser` policy. `Login.cshtml.cs` validates credentials by calling `director/isauthenticated` through the API, then signs in the cookie. A 401 from the API throws `BacklotApiUnauthorizedException`, which `SafeApiCall` in `AuthenticatedPageModel` catches and turns into a redirect to `/Login` with a `Turbo-Visit-Control: reload` header (forces a full page reload, not a frame-scoped redirect).

**Turbo conventions:** Use the base classes in `Pages/`:
- `AuthenticatedPageModel` — call `SetUserContext()` in every handler that renders `_Layout.cshtml`; use `SafeApiCall()` for API calls to handle 401 uniformly.
- `TurboEditPageModel` — use `TurboRedirect(url)` (emits HTTP 303) after a successful POST so Turbo Drive follows the redirect. Use `TurboInvalidPage()` (emits HTTP 422) to re-render the form with errors so Turbo swaps the body. Returning a plain `Page()` (200) on POST success causes Turbo to discard the response silently.

**`PlayAllowingClientErrorAsync`:** Used for write operations where the API may return a structured 4xx body (e.g., validation failures). Deserializes the error body instead of throwing, so per-field results reach the view. Auth errors (401/403) and 5xx still throw.

### Configuration

`BacklotApi:BaseUrl` in `appsettings.json` (or user-secrets for development). Session timeout and cookie auth expiry are both set to 8 hours with sliding expiration.

### Frontend

No npm or build pipeline. Assets are served from:
- `wwwroot/lib/` — vendored via LibMan (Turbo, Tailwind, etc.)
- CDN `<script>` tags in `_Layout.cshtml` for Scalar API reference

The Scalar side panel is marked `data-turbo-permanent` so it survives Turbo navigation. Do not put it inside a Turbo frame or a Turbo-swapped region.
