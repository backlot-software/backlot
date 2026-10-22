using Autofac.Extensions.DependencyInjection;
using Backlot.Demo.Web;
using Backlot.Demo.Web.Services;
using Backlot.Http;
using Backlot.Http.DependencyInjection.Autofac;
using Backlot.Http.Middleware;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.WebApp;

var builder = WebApplication.CreateBuilder(args);

// The Development screen in Studio calls the Chat/HandleMessage scenario. The client keeps the
// Mistral API key on this server; it is never exposed to the Studio browser or desktop process.
builder.Services.AddHttpClient<ICodeGenAgentService, CodeGenAgentService>(client =>
    client.Timeout = TimeSpan.FromSeconds(120));

// Backlot Studio is mounted by BuildWebApp; it is configured through the "BacklotStudio" section of
// appsettings.json. There is no separate Studio process.
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
});

app.UseMiddleware<AspNetMiddleware<AutofacScopeExecutor>>();
app.UseMiddleware<AspNetMiddleware<Defender>>();
app.UseMiddleware<AspNetMiddleware<AuthenticationInitializer>>();
app.UseMiddleware<AspNetMiddleware<SerilogContextEnrichment>>();

app.Run();
