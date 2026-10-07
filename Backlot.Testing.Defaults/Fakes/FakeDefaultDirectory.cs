using Autofac;
using Backlot.Core.Services;
using Backlot.DependencyInjection.Autofac;
using Microsoft.Extensions.Configuration;
using IConfigurationManager = Backlot.Core.Services.IConfigurationManager;

namespace Backlot.Testing.Defaults.Fakes;

public class FakeDefaultDirector(IFileSystem fileSystem, 
    IConfigurationManager configurationManager, 
    ContainerBuilder builder)
    : AutofacContainerDirector(fileSystem, configurationManager, builder)
{
    protected readonly string SecretKey = "T0PS3CR3TAN8T3RC3SP0T";
    
    public override void Registration()
    {
        // todo: fake user; Builder.RegisterType<BuiltInUserContext>().As<IUserContext>();
    }

    public override void Incept()
    {
        // examples; AssignInstructorFor<IPermission>(Instructors.AliasInitializer);
        // examples; AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization);
    }
}