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
    // optional: hostBuilder.ConfigureServices((ctx, collection) =>
    // {
    //     collection.AddLogging(lb => lb
    //         .AddSerilog(ctx.Configuration,
    //             cfg => cfg.WriteTo.Seq("http://localhost:5341"),
    //             //.AzureTableStorage(ctx.Configuration["Backlot.BlobConnectionString"]),
    //             level: Enum.TryParse(ctx.Configuration["Backlot.LogLevel"], out LogEventLevel l)
    //                 ? l
    //                 : LogEventLevel.Debug
    //         )
    //     );
    // });
    
});

app.UseMiddleware<AspNetMiddleware<AutofacScopeExecutor>>();
app.UseMiddleware<AspNetMiddleware<Defender>>();
app.UseMiddleware<AspNetMiddleware<AuthenticationInitializer>>();
// optional: app.UseMiddleware<AspNetMiddleware<SerilogContextEnrichment>>();

app.Run();
