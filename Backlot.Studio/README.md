# Backlot Studio

The management frontend for the Backlot API: browse and inspect roles, follow their relations,
explore every registered scenario with request and response examples, edit configuration, and fire
raw API requests from a built-in tester.

**All code is subject to the same licensing principles as the rest of the codebase.**

## Usage

Backlot Studio ships as a Razor Class Library. It embeds its own Razor Pages and static assets, and
`Backlot.WebApp` mounts it for you — if your host builds its app with `BuildWebApp`, the Studio is
already there:

```csharp
// Program.cs of the host
var app = builder.BuildWebApp(hostBuilder => { /* ... */ });
app.Run();
```

Browse to `/studio` and sign in with a Backlot API user.

To adjust it from code, pass a delegate; it is applied on top of the `BacklotStudio` configuration
section:

```csharp
var app = builder.BuildWebApp(hostBuilder => { /* ... */ }, enableHttps: true,
    configureStudio: studio => studio.PathPrefix = "/admin");
```

### Mounting it in another host

A host that does not use `BuildWebApp` mounts the Studio with two calls:

```csharp
builder.Services.AddBacklotStudio(builder.Configuration);

var app = builder.Build();
// ... the host's own middleware/endpoints ...
app.MapBacklotStudio("/studio");   // after UseHttpsRedirection, if the host redirects
app.Run();
```

`AddBacklotStudio` throws when called twice, so a host that uses `BuildWebApp` must **not** also
call it — use the `configureStudio` argument instead. That is deliberate: silently dropping one of
two configurations is far harder to diagnose than failing at startup.

### Configuration

`AddBacklotStudio` binds the `BacklotStudio` configuration section (when the `IConfiguration`
overload is used) and then applies the inline delegate on top:

```jsonc
{
  "BacklotStudio": {
    "PathPrefix": "/studio",  // mount path; MapBacklotStudio("/x") overrides it
    "BaseUrl": ""             // absolute URL of the Backlot API; leave empty when co-hosted
  }
}
```

`BaseUrl` is optional. Left empty — the default — the Studio resolves the API address per request
from the server's own listening addresses, so a local run on any port, and a container binding
whatever the platform hands it, both work with no configuration. Set it only to point the Studio at
an API running in a different process.

Further knobs on `BacklotStudioOptions`: `IdleTimeout`, `CookieSecurePolicy`, and
`ConfigureCookie` / `ConfigureSession` escape hatches. Note that `BuildWebApp` relaxes
`CookieSecurePolicy` to `SameAsRequest` when it is called with `enableHttps: false` in the
Development environment — otherwise a Secure-only cookie would never come back over plain HTTP and
sign-in would loop. Outside Development the configured policy is left alone.

### Isolation from the host

The Studio never touches the host's own authentication, authorization, session or static files. It
registers its own cookie scheme (`BacklotStudioDefaults.AuthenticationScheme`), a named
authorization policy scoped to the `Studio` area, cookies scoped to the mount path, and serves its
embedded `wwwroot` from `{prefix}/assets`. Mounting the Studio does not change how the host's
existing endpoints behave.
