using Backlot.Studio;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

// If no explicit URLs are passed, default to an ephemeral port on loopback (127.0.0.1:0)
if (string.IsNullOrEmpty(builder.Configuration["urls"]) &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://127.0.0.1:0");
}

const string BaseUrlKey = $"{BacklotStudioOptions.SectionName}:{nameof(BacklotStudioOptions.BaseUrl)}";
var baseUrl = builder.Configuration[BaseUrlKey];

if (string.IsNullOrWhiteSpace(baseUrl))
{
    Console.Error.WriteLine(
        $"{BaseUrlKey} is required: Backlot.Studio.Desktop runs Backlot Studio headless targeting a remote Backlot API. " +
        "Pass it via --BacklotStudio:BaseUrl=<url> or the BacklotStudio__BaseUrl environment variable.");
    return 1;
}

builder.Services.AddBacklotStudio(builder.Configuration, studio =>
{
    // The desktop sidecar runs over HTTP on loopback (127.0.0.1), so relax cookie policy to SameAsRequest
    // so authentication cookies function without requiring local TLS certificates.
    studio.CookieSecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Watch parent process or standard input EOF to ensure child exits if parent Electron process terminates unexpectedly
var parentPidStr = builder.Configuration["parent-pid"];
if (int.TryParse(parentPidStr, out var parentPid))
{
    _ = Task.Run(async () =>
    {
        while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var parent = System.Diagnostics.Process.GetProcessById(parentPid);
                if (parent.HasExited)
                {
                    Environment.Exit(0);
                }
            }
            catch
            {
                Environment.Exit(0);
            }
            await Task.Delay(1000);
        }
    });
}

if (Console.IsInputRedirected)
{
    _ = Task.Run(() =>
    {
        try
        {
            using var stream = Console.OpenStandardInput();
            while (stream.ReadByte() != -1) { }
        }
        catch
        {
        }
        Environment.Exit(0);
    });
}

// Emit readiness token once Kestrel is listening
app.Lifetime.ApplicationStarted.Register(() =>
{
    var server = app.Services.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>();
    var address = addressFeature?.Addresses.FirstOrDefault();

    if (address != null && Uri.TryCreate(address, UriKind.Absolute, out var uri))
    {
        Console.WriteLine($"BACKLOT_STUDIO_READY:{uri.Port}");
        Console.Out.Flush();
    }
    else
    {
        Console.Error.WriteLine("Error: Failed to determine listening address and port.");
    }
});

var studioOptions = app.Services.GetRequiredService<BacklotStudioOptions>();
app.MapBacklotStudio();

if (studioOptions.PathPrefix.Length > 0)
{
    app.MapGet("/", () => Results.Redirect(studioOptions.PathPrefix));
}

app.Run();
return 0;
