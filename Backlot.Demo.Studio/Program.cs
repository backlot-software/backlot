using Backlot.Studio;

var builder = WebApplication.CreateBuilder(args);

// A Studio-only host has no API in its own process, so the endpoint has to be configured. Checked
// here rather than left to AddBacklotStudio: an empty BaseUrl means "the API is this same host",
// which makes the Studio resolve this process's own address and call itself. That fails per request,
// far away from the cause. Failing at startup names the setting instead.
const string BaseUrlKey = $"{BacklotStudioOptions.SectionName}:{nameof(BacklotStudioOptions.BaseUrl)}";

if (string.IsNullOrWhiteSpace(builder.Configuration[BaseUrlKey]))
{
    throw new InvalidOperationException(
        $"{BaseUrlKey} is required: this host runs Backlot Studio on its own, so there is no " +
        "co-hosted Backlot API to fall back to. Set it in appsettings.json, in the environment as " +
        "BacklotStudio__BaseUrl, via 'dotnet user-secrets set', or on the command line as " +
        "--BacklotStudio:BaseUrl=https://your-api.example.com");
}

builder.Services.AddBacklotStudio(builder.Configuration, studio =>
{
    // Same reason as ApplicationBuilding.BuildWebApp gives: over plain http a Secure-only cookie is
    // never sent back and signing in loops to the login page forever. Only relaxed in Development,
    // because outside it a plain-http deployment is normally a proxy terminating TLS in front, where
    // downgrading the cookie would be a real weakening.
    if (builder.Environment.IsDevelopment())
        studio.CookieSecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// The mount path, after configuration and the delegate above have both had their say.
var studioOptions = app.Services.GetRequiredService<BacklotStudioOptions>();

if (!app.Environment.IsDevelopment())
{
    // The Studio's own Error page is @page "/error" inside the Studio area, so it lives under the
    // mount path once the area route convention has prefixed it.
    app.UseExceptionHandler($"{studioOptions.PathPrefix}/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Adds the Studio's embedded static assets, a session branched onto its mount path, and its Razor
// Pages. Routing, authentication and authorization middleware are added automatically by
// WebApplication because AddBacklotStudio registered those services -- adding them by hand here
// would also un-branch the session.
app.MapBacklotStudio();

// The only endpoint this host owns. It serves nothing but the Studio, so the root is a signpost.
if (studioOptions.PathPrefix.Length > 0)
    app.MapGet("/", () => Results.Redirect(studioOptions.PathPrefix));

app.Run();
