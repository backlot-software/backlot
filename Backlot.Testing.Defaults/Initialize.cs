using Autofac;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core.DependencyInjection;
using Backlot.DependencyInjection.Autofac;
using Backlot.Testing.Defaults.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IConfigurationManager = Backlot.Core.Services.IConfigurationManager;

namespace Backlot.Testing.Defaults;

public static class Initialize
{
    /// <summary>
    /// Builds the Autofac container and configures the Backlot service locator.
    /// </summary>
    /// <param name="builder">Configures service registrations before building the container.</param>
    public static void BuildApp(Action<ContainerBuilder> builder)
    {
        var b = new ContainerBuilder();
        builder(b);
        
        // building
        var serviceProvider = new AutofacServiceProvider(b.Build());
        ServiceLocator.Configure(serviceProvider);
    }

    /// <summary>
    /// Configures a default Backlot test environment.
    /// Uses FakeDefaultDirector, FakeUserContext, FakeStorage and FakeConfiguration.
    /// Registers default logging and caching services.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="settings">Optional values for FakeConfiguration.</param>
    /// <param name="configureContainer">Optional service registrations and overrides.</param>
    /// <returns>The configured container builder.</returns>
    public static ContainerBuilder ConfigureBacklot(this ContainerBuilder builder,
        IDictionary<string, string?>? settings = null,
        Action<ContainerBuilder>? configureContainer = null)
    {
        var configuration = new FakeConfiguration(settings ?? new Dictionary<string, string?>());

        return builder.ConfigureBacklot(
            (config, b) => new FakeDefaultDirector(
                new FakeStorage(), config, b),
            configureContainer,
            configuration);
    }
    
    /// <summary>
    /// Configures a Backlot test environment with custom director
    /// Registers default services and applies custom registrations
    /// </summary>
    /// <param name="builder">The AutoFac container builder.</param>
    /// <param name="createDirector">Create your own custom test director. Director must receive an IFileSystem implementation.
    /// Can use FakeStorage for in-memory file operations.</param>
    /// <param name="configureContainer">Some defaults are always registered first. Optional additional service registrations or overrides.
    /// Prefer using the custom Director's Registration() method for feature-specific service registrations.</param>
    /// <param name="configuration">Optional custom configuration manager, uses FakeConfiguration by default.</param>
    /// <typeparam name="TDirector">The custom test director type</typeparam>
    /// <returns>The configured container builder</returns>
    public static ContainerBuilder ConfigureBacklot<TDirector>(this ContainerBuilder builder, 
        Func<IConfigurationManager, ContainerBuilder, TDirector> createDirector,
        Action<ContainerBuilder>? configureContainer=null,
        IConfigurationManager? configuration=null)
        where TDirector: AutofacContainerDirector
    {
        configuration ??= new FakeConfiguration(new Dictionary<string, string>());
        
        // Register default services
        
        // Register default options required by MemoryCache.
        builder.RegisterInstance(Options.Create(new MemoryCacheOptions()))
            .As<IOptions<MemoryCacheOptions>>();
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
        
        // Create the director and register its services
        var d = createDirector(configuration, builder);
        d.Registration();
        d.Incept();
        
        // Apply custom registrations after defaults to allow overrides
        configureContainer?.Invoke(builder);

        return builder;
    }
}