# Backlot.Authentication.Basic

Basic authentication for Backlot, and the reference for writing your own `Backlot.Authentication.*` library.

## How authentication works

Each installation registers exactly **one** `IUserContext`. Its `AuthScheme` decides everything a client needs to know:

| Scheme   | Client contract |
|----------|-----------------|
| `Basic`  | Send `Authorization: Basic base64(username:password)` with every request. Validate a login with `IsAuthenticated`. |
| `Bearer` | Backlot issues the tokens (authorization code flow with PKCE), see [Backlot.Authentication.Bearer](../Backlot.Authentication.Bearer/README.md). Whether Backlot handles the login itself or acts as gateway to an external identity provider is invisible to clients. |

The request flow:

1. `AuthenticationInitializer` (Backlot.Http) reads the `Authorization` header.
2. If the scheme matches `IUserContext.AuthScheme`, it calls `IUserContext.Intialize(token)`.
   A different or missing scheme leaves the user anonymous.
3. Scenarios check access against the `IUserContext`. Unauthorized requests get a 401 with
   `WWW-Authenticate: <AuthScheme>`.
4. Clients can call the open `AuthInfo` scenario (`GET /api/role/director/authinfo`) to read the scheme before logging in.

## Guidelines for an authentication library

- **Implement `IUserContext`** (see `BasicUserContext`). `Intialize` must never throw. On any
  failure, set `IsAuthenticated = false`.
- **Use `Basic` or `Bearer` as scheme.** Clients only know these two contracts. An external identity
  provider is supported through the Bearer gateway, not through a new scheme.
- **Stay stateless.** Token validation must not depend on in-memory server state, so that it
  works behind load balancers and on Azure Functions.
- **Open scenarios** (`Access.Open`) must never return secrets.
