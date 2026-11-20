using Autofac;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Instructing;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.TestModels;

namespace Backlot.Testing.Shared.Directors;

public class AliasDirector(
    IFileSystem fileSystem,
    IConfigurationManager configuration,
    ContainerBuilder builder)
    : FakeDefaultDirector(fileSystem, configuration, builder)
{
    public override void Incept()
    {
        AssignExpressionEngineFor<string, MustachExpressionEngine>();
        AssignInstructorFor<INumberBase>(Instructors.AliasInitializer);
        AssignInstructorFor<IFormulaGroup>(Instructors.AliasInitializer);
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization); 
    }
}