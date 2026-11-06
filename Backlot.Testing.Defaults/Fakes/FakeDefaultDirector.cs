using Autofac;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.DependencyInjection.Autofac;
using IConfigurationManager = Backlot.Core.Services.IConfigurationManager;

namespace Backlot.Testing.Defaults.Fakes;

/// <summary>
/// Provides default service registrations for Backlot tests.
/// Can be extended with project-specific registrations.
/// </summary>
/// <remarks>
/// Registers FakeUserContext as IUserContext per lifetime scope.
/// 
/// Call base.Registration() in custom directors to retain
/// the default registrations.
///
/// Register feature-specific services in the custom Director's `Registration()` method.
/// </remarks>

/// <param name="fileSystem">Handles file operations. FakeStorage is available for this purpose, but you can provide another implementation.</param>
/// <param name="configurationManager">Provides configuration values. FakeConfiguration is used by default.</param>
/// <param name="builder">Allows the Director to register services</param>
public class FakeDefaultDirector(IFileSystem fileSystem, 
    IConfigurationManager configurationManager, 
    ContainerBuilder builder)
    : AutofacContainerDirector(fileSystem, configurationManager, builder)
{
    protected readonly string SecretKey = "T0PS3CR3TAN8T3RC3SP0T";
    
    /// <summary>
    /// Registers services and dependencies in the Autofac container, such as repositories, fakes, mocks, or other feature-specific services.
    /// </summary>
    public override void Registration()
    {
        Builder.RegisterType<FakeUserContext>().As<IUserContext>().InstancePerLifetimeScope();
    }

    /// <summary>
    /// Configures Backlot behavior, such as role instructors, expression engines, watchers, and scenario compositions.
    /// </summary>
    public override void Incept()
    {
        // examples; AssignInstructorFor<IPermission>(Instructors.AliasInitializer);
        // examples; AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization);
    }
}