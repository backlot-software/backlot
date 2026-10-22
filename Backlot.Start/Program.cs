using Autofac.Extensions.DependencyInjection;
using Backlot.Start;
using Backlot.Http;
using Backlot.Http.DependencyInjection.Autofac;
using Backlot.Http.Middleware;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.WebApp;

var builder = WebApplication.CreateBuilder(args);
var app = builder.BuildWebApp(hostBuilder =>
{
    hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    hostBuilder.ConfigureBacklotWeb((_, b) =>
    {
        var fs = new LocalDiskStorage();
        return new WebDirector(
            fileSystem: fs,
            configurationManager: new JsonSettingsManager("local", fs),
            // or use; new DuplexConfigurationSettingsManager(context.Configuration, jsonSettingsManager);
            builder: b);
    });
});

app.UseMiddleware<AspNetMiddleware<AutofacScopeExecutor>>();
app.UseMiddleware<AspNetMiddleware<Defender>>();
app.UseMiddleware<AspNetMiddleware<AuthenticationInitializer>>();
// optional: app.UseMiddleware<AspNetMiddleware<SerilogContextEnrichment>>();

app.Run();
