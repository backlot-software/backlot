# Backlot.Authentication.Razor

Sign in to a Backlot API from an ASP.NET Core host, once, for the whole host. Backlot Studio uses it
for its login, and a solution's own Razor Pages share the same sign-in: an operator who signed in at
`/studio` is signed in on the solution's pages too, and the other way around.

## What it provides

- The login pages: `{prefix}/login` (Basic), `{prefix}/login/bearer` (Bearer, through the identity
  provider configured with Backlot.Authentication.Bearer) and `{prefix}/logout`.
- A cookie authentication scheme and authorization policy, both named `Backlot`
  (`BacklotSignInDefaults`). Neither becomes the host's default: the host opts its pages in.
- `IBacklotApiClient`: calls the Backlot API **as the signed-in user**. The API credential (Basic
  credential or Bearer tokens) lives in the server-side session and never reaches the browser; an
  expired access token is refreshed automatically.
- `IBacklotSignInManager`: the sign-in flow itself, for a host that builds its own login page.
- `BacklotPageModel`: base class for pages calling the API, turning a rejected credential into a
  redirect to the login page.

## Usage

### With Backlot Studio

Nothing to add: `AddBacklotStudio` / `BuildWebApp` register and mount the sign-in. Its login pages
stay under the Studio path (`/studio/login`), so existing Bearer redirect URIs keep working. Put the
API address in the `BacklotSignIn` section (`BacklotStudio:BaseUrl` is still read when it is missing):

```jsonc
{
  "BacklotSignIn": {
    "BaseUrl": "https://localhost:7221"   // absolute URL of the Backlot API; REQUIRED
  }
}
```

Adjust the sign-in from code with `BuildWebApp(..., configureSignIn: o => ...)`, or
`AddBacklotStudio(configuration, configureSignIn: o => ...)`.

### Without Backlot Studio

```csharp
builder.Services.AddBacklotSignIn(builder.Configuration);

var app = builder.Build();
// ... the host's own middleware/endpoints ...
app.MapBacklotSignIn();   // login pages under /account unless PathPrefix says otherwise
app.Run();
```

`AddBacklotSignIn` may be called more than once (the Studio calls it too); later calls only apply
their configuration.

### Protecting the solution's own pages

```csharp
builder.Services.AddRazorPages(o => o.Conventions.RequireBacklotSignIn("/Portal"));
```

or `[Authorize(Policy = BacklotSignInDefaults.AuthorizationPolicy)]` on a page model. A user who is
not signed in is sent to the login page and returns to the page afterwards.

Call the API as the signed-in user by injecting `IBacklotApiClient`, preferably from a
`BacklotPageModel`:

```csharp
public class IndexModel(IBacklotApiClient api) : BacklotPageModel
{
    public object? Me { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();
        (Me, var redirect) = await SafeApiCall(() => api.WhoAmI());
        return redirect ?? Page();
    }
}
```

Sign out by posting to the logout page, `<form method="post" asp-area="BacklotSignIn" asp-page="/Logout">`
(optionally with a `returnUrl` field). It signs out of the Studio and the solution's pages alike.

### A login page of your own

Point `BacklotSignIn:LoginPath` at the page, e.g. `/portal/login`; every sign-in prompt (the
Studio's included) is then sent there with a `ReturnUrl`. Build it on `IBacklotSignInManager`:

- Basic: `SignInBasicAsync(username, password, returnUrl)`.
- Bearer: `StartBearerAsync(returnUrl, doneUrl)` and, on the done url, `CompleteBearerAsync(code, state, error, doneUrl)`.
  The done url must be in the API's `AllowedRedirectUris`. The built-in `LoginBearer` page shows
  how to hop from the cross-site redirect back to a same-site request before completing.
- `GetApiAuthSchemeAsync()` tells which of the two the API uses.

Every result carries the url to continue to, or the error to show.

### Single sign-on and the host

The sign-in cookies (`.Backlot.Auth`, `.Backlot.Session`) are scoped to `/`, and `MapBacklotSignIn`
runs the session for every request of the host. That session is the host's session: a host using
the Backlot sign-in must not add a `UseSession` of its own. Nothing else of the host changes: its
default authentication scheme and the authorization of its other endpoints stay as they were.

### Bearer redirect URIs

The API's allowed redirect URIs need to include `https://<host>{prefix}/login/bearer?handler=Done`,
e.g. `https://<host>/studio/login/bearer?handler=Done` when mounted by the Studio. For Backlot Studio
Desktop that is `http://127.0.0.1/studio/login/bearer?handler=Done` (any port).
