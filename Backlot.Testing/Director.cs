using Autofac;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Instructing;
using Backlot.DependencyInjection.Autofac;
using Backlot.Testing.Core;

namespace Backlot.Testing;

public class Director(IFileSystem fileSystem, IConfigurationManager config, ContainerBuilder builder)
    : AutofacContainerDirector(fileSystem, config, builder)
{
    public override void Registration()
    {
        throw new System.NotImplementedException();
        // mock implementations are added to the container by specific test [setup]s.
    }

    public override void Incept()
    {
        AssignInstructorFor<IMoney>(MoneyInitialization.Initialize);
        AssignInstructorFor<ICard>(CardInitialization.Initialize);
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization); 
        AssignInstructorFor<IPerson>(Instructors.AliasInitializer);
    }
}

public class AliasDirector(IFileSystem fileSystem, IConfigurationManager config, ContainerBuilder builder)
    : AutofacContainerDirector(fileSystem, config, builder)
{
    public override void Registration()
    {
        throw new System.NotImplementedException();
        // mock implementations are added to the container by specific test [setup]s.
    }

    public override void Incept()
    {
        AssignExpressionEngineFor<string, MustachExpressionEngine>();
        AssignInstructorFor<INumberBase>(Instructors.AliasInitializer);
        AssignInstructorFor<IFormulaGroup>(Instructors.AliasInitializer);
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization); 
    }
}