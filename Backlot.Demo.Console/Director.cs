using Autofac;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Instructing;
using Backlot.Demo.Console.Roles;
using Backlot.DependencyInjection.Autofac;
using Backlot.Services.LiteDB;

namespace Backlot.Demo.Console;

public class Director(IFileSystem fileSystem, IConfigurationManager config, ContainerBuilder builder)
    : AutofacContainerDirector<
        LiteRelationRepository, 
        LitePersistedRoleRepository, 
        DummyUnitOfWork, 
        UserCtx, 
        CacheFactory>
        (fileSystem, config, builder)
{
    protected override string SecretKey => "1234567890ABCDEF";

    public override void Incept()
    {
        AssignInstructorFor<IPermission>(Instructors.AliasInitializer);
        AssignInstructorFor<IMoney>(MoneyInitialization.Initialize);
        AssignInstructorFor<ILineItem>(LineItemInitialization.Initialize);
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization); 
    }
}