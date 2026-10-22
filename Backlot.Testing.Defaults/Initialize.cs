using Autofac;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core.DependencyInjection;
using Backlot.DependencyInjection.Autofac;
using Backlot.Testing.Defaults.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using IConfigurationManager = Backlot.Core.Services.IConfigurationManager;

namespace Backlot.Testing.Defaults;

public static class Initialize
{
    public static void BuildApp(Action<ContainerBuilder> builder)
    {
        var b = new ContainerBuilder();
        builder(b);
        
        // building
        var serviceProvider = new AutofacServiceProvider(b.Build());
        ServiceLocator.Configure(serviceProvider);
    }
    
    /// <summary>
    /// Configure a test app
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="createDirector">Create your own test director, tests, using this builder always use the FakeConfiguration implementation based on a dictionary.</param>
    /// <param name="configureContainer">when null it will register some defaults, remember it's better to use your directors.registration for it.</param>
    /// <typeparam name="TDirector"></typeparam>
    /// <returns></returns>
    public static ContainerBuilder ConfigureBacklot<TDirector>(this ContainerBuilder builder, 
        Func<IConfigurationManager, ContainerBuilder, TDirector> createDirector,
        Action<ContainerBuilder>? configureContainer=null)
        where TDirector: AutofacContainerDirector
    {
        var configuration = new FakeConfiguration(new Dictionary<string, string>());
        if (configureContainer == null)

        {
            builder.RegisterType<MemoryCache>()
                .As<IMemoryCache>()
                .SingleInstance();
        
            builder.RegisterInstance(new LoggerFactory())
                .As<ILoggerFactory>();
            
            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .SingleInstance();
                
            // examples; builder.RegisterType<MediaFormatResolver>()
            // examples;     .As<IMediaFormatResolver>()
            // examples;     .InstancePerRequest();
            // examples; 
            // examples; // Default formatter.
            // examples; builder.RegisterType<JsonFormatter>()
            // examples;     .As<IMediaFormatter>()
            // examples;     .InstancePerRequest();
            // examples;     
            // examples; builder.RegisterType<PlainTextFormatter>()
            // examples;     .As<IMediaFormatter>()
            // examples;     .InstancePerRequest();
        }
        else
        {
            configureContainer(builder);
        }
        
        var d = createDirector(configuration, builder);
        d.Registration(); // register all defaults
        d.Incept();

        return builder;
    }
}