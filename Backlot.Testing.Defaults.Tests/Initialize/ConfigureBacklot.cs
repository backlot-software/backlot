using Autofac;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Backlot.Testing.Defaults.Tests.Initialize;

public class ConfigureBacklot
{
    // Custom Director helper
    private class CustomTestDirector(
        IFileSystem fileSystem,
        IConfigurationManager configurationManager,
        ContainerBuilder builder)
        : Fakes.FakeDefaultDirector(fileSystem, configurationManager, builder)
    {
        public override void Registration()
        {
            // Keep the default registrations
            base.Registration();
            
            // Add a custom service
            Builder.RegisterInstance(new TestService()).AsSelf();
        }
    }

    private class TestService()
    {
        
    }
    
    [Test]
    public void ConfigureBacklot_DefaultSetup_RegistersDefaultServices()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act
        builder.ConfigureBacklot();
        var container = builder.Build();

        // Assert
        Assert.Multiple(() =>
        {
            // uses FakeUserContext by default
            Assert.That(container.Resolve<IUserContext>(), Is.InstanceOf<Fakes.FakeUserContext>());
            
            // uses FakeConfiguration by default
            Assert.That(container.Resolve<IConfigurationManager>(), Is.InstanceOf<Fakes.FakeConfiguration>());
            
            // uses FakeStorage by default
            Assert.That(container.Resolve<IFileSystem>(), Is.InstanceOf<Fakes.FakeStorage>());
            
            // registers default services
            Assert.That(container.Resolve<IMemoryCache>(), Is.Not.Null);
            Assert.That(container.Resolve<ILoggerFactory>(), Is.Not.Null);
        });
    }

    [Test]
    public void ConfigureBacklot_CustomUserContext_OverridesDefault()
    {
        // Arrange
        var builder = new ContainerBuilder();

        var customUserContext = new Fakes.FakeUserContext()
        {
            UserName = "Test Jonas"
        };

        // Act
        builder.ConfigureBacklot(configureContainer: b =>
        {
            b.RegisterInstance(customUserContext).As<IUserContext>();
        });
        var container = builder.Build();
        var userContext = container.Resolve<IUserContext>();

        // Assert
        Assert.Multiple(() =>
        {
            // uses custom user context
            Assert.That(userContext, Is.SameAs(customUserContext));
            Assert.That(userContext.UserName, Is.EqualTo("Test Jonas"));
            
            // default services are still registered
            Assert.That(container.Resolve<IFileSystem>(), Is.InstanceOf<Fakes.FakeStorage>());
            Assert.That(container.Resolve<IMemoryCache>(), Is.Not.Null);
        });
    }

    [Test]
    public void ConfigureBacklot_CustomDirector_RegistersCustomServices()
    {
        // Arrange
        var builder = new ContainerBuilder();
        
        // Act
        builder.ConfigureBacklot(
            (config, b) => new CustomTestDirector(
                new Fakes.FakeStorage(), config, b));
        
        var container = builder.Build();
        
        // Assert
        Assert.Multiple(() =>
        {
            // Service registered by custom Director
            Assert.That(container.Resolve<TestService>(), Is.Not.Null);
            
            // efault director registration remains available
            Assert.That(container.Resolve<IUserContext>(), Is.InstanceOf<Fakes.FakeUserContext>());

            // efault Initialize services remain available
            Assert.That(container.Resolve<IMemoryCache>(), Is.Not.Null);
        });
    }
    
    [Test]
    public void ConfigureBacklot_CustomService_OverridesDefault()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var customLoggerFactory = new LoggerFactory();

        // Act
        builder.ConfigureBacklot(
            configureContainer: b =>
            {
                b.RegisterInstance(customLoggerFactory)
                    .As<ILoggerFactory>();
            });

        using var container = builder.Build();
        var loggerFactory = container.Resolve<ILoggerFactory>();

        // Assert
        Assert.Multiple(() =>
        {
            // custom service is used
            Assert.That(loggerFactory, Is.SameAs(customLoggerFactory));

            // default services remain available.
            Assert.That(container.Resolve<IMemoryCache>(), Is.Not.Null);
            Assert.That(container.Resolve<ILogger<ConfigureBacklot>>(), Is.Not.Null);
        });
    }
}