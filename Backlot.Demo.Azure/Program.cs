// ReSharper disable RedundantUsingDirective : we are not doing this for demo projects, to be able to switch quickly with implementations.
using System;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Extensions.DependencyInjection;
using Backlot.Authentication.BuiltIn;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.DependencyInjection.Autofac;
using Backlot.Http;
using Backlot.Http.Middleware;
using Backlot.Functions;
using Backlot.Functions.Defaults;
using Backlot.Functions.Services;
using Backlot.Http.DependencyInjection.Autofac;
using Backlot.Services.Filesystem;
using Backlot.Services.Filesystem.BlobStorage;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Backlot.Services.RavenDb;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;


namespace Backlot.Demo.Azure
{
    public static class Program
    {
        public static async Task Main()
        {
            var host = new HostBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<FunctionsHttpMiddleware<AutofacScopeExecutor>>(); // always identify Autofac first.
                    worker.UseMiddleware<FunctionsHttpMiddleware<Defender>>();
                    worker.UseMiddleware<FunctionsHttpMiddleware<AuthenticationInitializer>>();
                    worker.UseMiddleware<FunctionsHttpMiddleware<SerilogContextEnrichment>>();
                })
                //.BacklotAppConfiguration<MemoryRelationRepository, MemoryPersistedRoleRepository, DummyUnitOfWork> (
                .ConfigureBacklotWeb((configuration, builder) =>
                {
                    var fs = new LocalDiskStorage();
                    var jsonSettingsManager = new JsonSettingsManager(configuration["Backlot.Environment"] ?? "local", fs);
                    var duplexSettingsManager = new DuplexConfigurationSettingsManager(configuration, jsonSettingsManager);
                    return new AzureDirector(fs, duplexSettingsManager, builder);
                } )
                //.ConfigureServices((_, collection) => { collection.AddLogging(lb => lb.AddSerilog(cfg => cfg.WriteTo.Seq("http://localhost:5341")));}) //step 4: logging is initialized
                .ConfigureServices((ctx, collection) => { collection.AddLogging(lb => lb
                    .AddSerilog(ctx.Configuration,
                        cfg => cfg.WriteTo.Seq("http://localhost:5341"),
                                //.AzureTableStorage(ctx.Configuration["Backlot.BlobConnectionString"]),
                        level: Enum.TryParse(ctx.Configuration["Backlot.LogLevel"], out LogEventLevel l) ? l : LogEventLevel.Debug
                        )
                    );
                })
                .Build();

            await host.RunAsync();
        }
    }
}