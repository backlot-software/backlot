using System;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core;
using Backlot.DependencyInjection.Autofac;
using Backlot.Http.Media;
using Backlot.Http.Media.Formatters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Backlot.Http.DependencyInjection.Autofac;

public static class WebBuilderExtensions
{
    /// <summary>
    /// Build a Backlot web application with the given configuration
    /// And does use the default Autofac container
    /// </summary>
    /// <param name="hostBuilder">The current hostbuilder</param>
    /// <param name="createDirector">Initialize the Director singleton.</param>
    /// <returns></returns>
    public static IHostBuilder ConfigureBacklotWeb<TDirector>(
        this IHostBuilder hostBuilder, 
        Func<IConfiguration, ContainerBuilder, TDirector> createDirector)
        where TDirector: AutofacContainerDirector
    {
        Loader.PreLoad();

        return hostBuilder
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.AddEnvironmentVariables();
                builder.Build();
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((_, collection) =>
            {
                collection.AddOptions();
                collection.AddHttpClient();
            })
            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                builder.RegisterType<MemoryCache>()
                    .As<IMemoryCache>()
                    .SingleInstance();
                
                builder.RegisterType<MediaFormatResolver>()
                    .As<IMediaFormatResolver>()
                    .InstancePerRequest();
        
                // Default formatter.
                builder.RegisterType<JsonFormatter>()
                    .As<IMediaFormatter>()
                    .InstancePerRequest();
                
                builder.RegisterType<PlainTextFormatter>()
                    .As<IMediaFormatter>()
                    .InstancePerRequest();
                
                var d = createDirector(context.Configuration, builder);
                d.Registration(); // register all defaults

                // 3.0) -- Load user/application configuration from a container stream.

                var modules = new[]
                {
                    new ConfigurationModule(context.Configuration), //basic configuration
                    // Json configuration https://autofac.readthedocs.io/en/latest/configuration/xml.html#configuring-with-microsoft-configuration-4-0
                    new ConfigurationModule(new ConfigurationBuilder() //json configuration
                        .AddJsonStream(d.ConfigurationManager.GetContainerStream()).Build())
                };

                foreach (var module in modules) // Register modules
                {
                    builder.RegisterModule(module);
                }

                // Make your Autofac registrations. Order is important!
                // If you make them BEFORE you call Populate, then the
                // registrations in the ServiceCollection will override Autofac
                // registrations; if you make them AFTER Populate, the Autofac
                // registrations will override. You can make registrations
                // before or after Populate, however you choose.

                // ... AUTOFAC registrations here ...

                // --------------------------------------------->
                // THE INCEPTION OF THE APPLICATION GUIDED BY THE DIRECTOR

                d.Incept();

                // <---------------------------------------------
            });
    }

}