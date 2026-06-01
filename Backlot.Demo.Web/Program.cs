using Autofac.Extensions.DependencyInjection;
using Backlot.Demo.Web;
using Backlot.Http;
using Backlot.Http.DependencyInjection.Autofac;
using Backlot.Http.Middleware;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.WebApp;

var builder = WebApplication.CreateBuilder(args);
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

app.Run();