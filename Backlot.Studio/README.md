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

Browse to `/studio` and sign in with a Backlot API user. Signing in is done by
[Backlot.Authentication.Razor](../Backlot.Authentication.Razor/README.md), which the Studio
registers for you; the same sign-in covers the host's own Razor Pages (single sign-on).

To adjust it from code, pass a delegate; it is applied on top of the `BacklotStudio` configuration
section:

```csharp
var app = builder.BuildWebApp(hostBuilder => { /* ... */ }, enableHttps: true,
    configureStudio: studio => studio.PathPrefix = "/admin",
    configureSignIn: signIn => signIn.IdleTimeout = TimeSpan.FromHours(1));
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

`Backlot.Demo.Studio/` in this repository is a runnable example of exactly this: a host that mounts
only the Studio and points it at a Backlot API in another process through `BaseUrl`.

`AddBacklotStudio` throws when called twice, so a host that uses `BuildWebApp` must **not** also
call it — use the `configureStudio` argument instead. That is deliberate: silently dropping one of
two configurations is far harder to diagnose than failing at startup.

### Configuration

`AddBacklotStudio` binds the `BacklotStudio` configuration section and the `BacklotSignIn` section
(when the `IConfiguration` overload is used) and then applies the inline delegates on top:

```jsonc
{
  "BacklotStudio": {
    "PathPrefix": "/studio"               // mount path; MapBacklotStudio("/x") overrides it
  },
  "BacklotSignIn": {
    "BaseUrl": "https://localhost:7221"   // absolute URL of the Backlot API; REQUIRED
  }
}
```

`BaseUrl` is **required** and has no fallback: the address is never inferred from the server's own
listening addresses, so it must be set even when the API is co-hosted with the Studio. For existing
configurations `BacklotStudio:BaseUrl` is still read when `BacklotSignIn:BaseUrl` is missing. A
missing or non-absolute value is not a startup failure — the host still comes up and the login page
reports `BaseUrl not configured correctly on host` through its validation summary, and every API
call throws until it is set.

The sign-in knobs (`IdleTimeout`, `CookieSecurePolicy`, `LoginPath`, `ConfigureCookie` /
`ConfigureSession`) live on `BacklotSignInOptions`. Note that `BuildWebApp` relaxes
`CookieSecurePolicy` to `SameAsRequest` when it is called with `enableHttps: false` in the
Development environment — otherwise a Secure-only cookie would never come back over plain HTTP and
sign-in would loop. Outside Development the configured policy is left alone.

### Sharing the sign-in with the host

The Studio's pages sit in their own `Studio` area, behind the `Backlot` authorization policy, and
its embedded `wwwroot` is served from `{prefix}/assets`. The sign-in itself is shared with the host
on purpose: its cookies are scoped to `/` and its session runs for every request, so a signed-in
operator is also signed in on the host's own pages that require the `Backlot` policy, and signing
out of one signs out of both. The host's default authentication scheme and its other endpoints are
left alone. See [Backlot.Authentication.Razor](../Backlot.Authentication.Razor/README.md) for
protecting the host's pages, calling the API as the signed-in user, and using a login page of the
host's own.

### Signing in

The login page follows the scheme the Backlot API reports through its `AuthInfo` scenario:

- **Basic**: `{prefix}/login` asks for username and password and sends them with every API call.
- **Bearer**: `{prefix}/login` forwards to `{prefix}/login/bearer`, which signs in through the identity
  provider using the Backlot.Authentication.Bearer additional package. The access and refresh tokens are kept
  in the session; an expired access token is refreshed automatically.

The login pages are mounted under the Studio's path unless `BacklotSignIn:PathPrefix` says otherwise.
For more information how to setup Backlot.Authentication.Bearer, check its documentation.
- For Backlot Studio Desktop the allowed redirect URIs need to include `http://127.0.0.1/studio/login/bearer?handler=Done` (any port).
- For a hosted Studio the allowed redirect URIs need to include `https://<host>/studio/login/bearer?handler=Done` (where `<host>` is the host name of the web application, which can be (but is not required to be) the same as Backlot API).
