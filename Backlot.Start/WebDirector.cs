using Autofac;
using Backlot.Authentication.Basic;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Scenarios.Authentication;
using Backlot.Defaults.Services;
using Backlot.DependencyInjection.Autofac;
using Backlot.Http.Watching;
using Backlot.Services.LiteDB;
using Newtonsoft.Json.Linq;
using IConfigurationManager = Backlot.Core.Services.IConfigurationManager;

namespace Backlot.Start;

public class WebDirector(IFileSystem fileSystem, IConfigurationManager configurationManager, ContainerBuilder builder)
    : AutofacContainerDirector<
            LiteRelationRepository,
            LitePersistedRoleRepository,
            DummyUnitOfWork,
            BasicUserContext,
            CacheFactory>
        (fileSystem, configurationManager, builder)
{

    public override void Registration()
    {
        base.Registration();

        // the IUserRepository is used by BasicUserContext
        Builder.RegisterType<UserFileRepository>()
            .As<IUserRepository>()
            .SingleInstance();
    }


    public override void Incept()
    {
        // 1) -- Backlot assign expression engine for calculating refering values.

        // AssignExpressionEngineFor<string, MustachExpressionEngine>();

        // 2) -- Backlot instructors

        // AssignInstructorFor<IFormula>(Instructors.AliasInitializer);

        // Try to avoid calling the db by using the EncryptedPermissionInitialization. This allows clients to send a valid encrypted version of the permission.
        AssignInstructorFor<IPersist>(PermissionInitialization.EncryptedPermissionInitialization, 997);
        // Set DbAccessInitialization as default for for all roles that are persisted and are not already set by EncryptedPermissionInitialization.
        AssignInstructorFor<IPersist>(PermissionInitialization.DbAccessInitialization, 998);
        // When not persisted we set the default permission:
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization, 999);

        // 3) -- Backlot prepare scenarios for playing

        PrepareCompositionFor<WhoAmI>(scenario =>
        {
            scenario.GetInfo = async () =>
            {
                var ur = ServiceLocator.Get<IUserRepository>();
                var user = await ur.TryGetUser(UserContext.Current.UserName);
                if (user.success)
                {
                    var settings = JObject.Parse(user.settings);
                    settings.Remove("pw");
                    return settings;
                }

                return JObject.Parse(string.Empty);
            };
        });


        // -- here ...

        // 4) -- Backlot define watchers for all or per scenario.
        // f.e. WatchAll<DebugWatcher>();
    }

    protected override string SecretKey => "SECRET_<CHANGEME>";

}
