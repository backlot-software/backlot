# Important Notice:

**Backlot Studio is a low-level, experimental "vibe coded" UI.** on top op the Backlot API's. For scenario management, role inspection and system monitoring.

It is purpose is a playground for exploring vibe coding with the Backlot framework and it's Movie Pattern Practice. 

It is **NOT MEANT FOR PRODUCTION USE** in this stage. 

The focus is on demonstrating the "vibe" of building interfaces with MPP, rather than providing a production-hardened administration panel.
We plan to refactor this into a production ready interface as soon as we consider the code to be more maintainable and following best practices.

**All code is subject to the same licensing principles as the rest of the codebase.**

## Usage

Backlot Studio ships as a Razor Class Library. It embeds its own Razor Pages and static
assets, so a host application mounts the whole UI with two calls — the same shape as
`MapScalarApiReference`:

```csharp
// Program.cs of the host (e.g. the Backlot API web app)
builder.Services.AddBacklotStudio(builder.Configuration, studio =>
{
    studio.BaseUrl = "https://localhost:7221"; // the Backlot HTTP API the Studio talks to
});

var app = builder.Build();
// ... the host's own middleware/endpoints ...
app.MapBacklotStudio("/studio");             // mount the UI under /studio
app.Run();
```

Browse to `/studio` and sign in with a Backlot API user.

### Configuration

`AddBacklotStudio` binds the `BacklotStudio` configuration section (when the
`IConfiguration` overload is used) and then applies the inline delegate on top:

```jsonc
{
  "BacklotStudio": {
    "BaseUrl": "https://localhost:7221", // absolute URL of the Backlot API (required)
    "PathPrefix": "/studio"              // mount path; MapBacklotStudio("/x") overrides it
  }
}
```

Further knobs on `BacklotStudioOptions`: `IdleTimeout`, `CookieSecurePolicy` (relax to
`SameAsRequest` to sign in over plain HTTP locally), and `ConfigureCookie` / `ConfigureSession`
escape hatches.

### Isolation from the host

The Studio never touches the host's own authentication, authorization, session or static
files. It registers its own cookie scheme (`BacklotStudioDefaults.AuthenticationScheme`), a
named authorization policy scoped to the `Studio` area, cookies scoped to the mount path, and
serves its embedded `wwwroot` from `{prefix}/assets`. Mounting the Studio does not change how
the host's existing endpoints behave.