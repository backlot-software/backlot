using Autofac;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Testing.Defaults.Fakes;

namespace Backlot.Testing.Shared.Directors;

public class PermissionDirector(
    IFileSystem fileSystem,
    IConfigurationManager configuration,
    ContainerBuilder builder)
    : FakeDefaultDirector(fileSystem, configuration, builder)
{
    public override void Incept()
    {
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization);
    }
}