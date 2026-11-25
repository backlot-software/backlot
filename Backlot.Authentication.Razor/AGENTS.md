# AGENTS.MD

## Project Backlot.Authentication.Razor

Razor Class Library that signs users in to a Backlot API from an ASP.NET Core host, shared by Backlot
Studio and the host's own Razor Pages (single sign-on). See `README.md` for the mounting contract and
`BacklotSignInExtensions.cs` for the implementation.

- **Area `BacklotSignIn`** (`Areas/BacklotSignIn/Pages`) holds the login pages, so their routes
  (`@page "/login"`) and conventions (route prefix, anonymous access) never touch the host's pages.
  The area name never appears in a URL.
- **Flow logic lives in `IBacklotSignInManager`** (`Implementation/BacklotSignInManager.cs`), not in
  the page models, so a host's own login page signs in exactly like the built-in ones.
- **Auth**: follows the API scheme (AuthInfo scenario): Basic (username/password as base64 on every
  request) or Bearer (Backlot.Authentication.Bearer gateway flow); credentials and tokens live in the
  server-side session, read by `AuthHeaderHandler`. Session keys are internal to this assembly.
- **Scope**: cookies at path `/`, session for every request — that is what makes it a single
  sign-on. The scheme and policy are never the host's defaults.
- **Studio coupling**: only through `InternalsVisibleTo("Backlot.Studio")`, which sets the
  `PathPrefixFallback`/`DefaultReturnUrlFallback` so the login pages default to the Studio's path.

## Documentation Guidelines

- Use Markdown for formatting.
- Do not add property explanations in MD files, add those in code comments summaries.
