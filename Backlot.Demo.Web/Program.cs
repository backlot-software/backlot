using Autofac.Extensions.DependencyInjection;
using Backlot.Demo.Web;
using Backlot.Studio;
using Backlot.Http;
using Backlot.Http.DependencyInjection.Autofac;
using Backlot.Http.Middleware;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.WebApp;

var builder = WebApplication.CreateBuilder(args);

// Backlot Studio is mounted into this API host as a package — there is no separate Studio process.
// Registration has to happen before BuildWebApp, which is what calls builder.Build().
builder.Services.AddBacklotStudio(builder.Configuration, studio =>
{
#if DEBUG
    // Debug runs without HTTPS redirection (see the enableHttps argument below), so the Studio's
    // cookies must survive a plain-HTTP request or sign-in silently loops back to the login page.
    studio.CookieSecurePolicy = CookieSecurePolicy.SameAsRequest;
#endif
});

var app = builder.BuildWebApp(hostBuilder =>
{
    hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    hostBuilder.ConfigureBacklotWeb((c, b) =>
    {
        var fs = new LocalDiskStorage();
        return new WebDirector(
            fileSystem: fs,
            configurationManager: new JsonSettingsManager(c["Backlot.Environment"] ?? "local", fs),
            // new DuplexConfigurationSettingsManager(context.Configuration, jsonSettingsManager);
            builder: b);
    });
    
#if DEBUG
}, false);
#else
}, true);
#endif

app.UseMiddleware<AspNetMiddleware<AutofacScopeExecutor>>();
app.UseMiddleware<AspNetMiddleware<Defender>>();
app.UseMiddleware<AspNetMiddleware<AuthenticationInitializer>>();
app.UseMiddleware<AspNetMiddleware<SerilogContextEnrichment>>();

app.MapBacklotStudio();

app.Run();