using Autofac;
using Backlot.Authentication.BuiltIn;
using Backlot.Authentication.BuiltIn.Scenarios;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.Defaults.Instructing;
using Backlot.Defaults.Scenarios.Authentication;
using Backlot.Defaults.Services;
using Backlot.Demo.Azure.Roles;
using Backlot.Demo.Azure.Scenarios;
using Backlot.DependencyInjection.Autofac;
using Backlot.Http.Media;
using Backlot.Http.Media.Formatters.Csv;
using Backlot.Http.Media.Formatters.Html;
using Backlot.Http.Watching;
// optional, if you want to use the authentication.jwt library.
using Backlot.Services.Postmark;
using Backlot.Services.RavenDb;
using Newtonsoft.Json.Linq;

namespace Backlot.Demo.Azure;

public class AzureDirector(IFileSystem fileSystem, IConfigurationManager configurationManager, ContainerBuilder builder) : 
    AutofacContainerDirector<
        RavenRelationRepository, 
        RavenPersistedRoleRepository, 
        RavenUnitOfWork, 
        BuiltInUserContext,
        CacheFactory>(fileSystem, configurationManager, builder)
{
    protected override string SecretKey => "dev_E76548691F3A49DAB38B136EC1E95B21";

    public override void Incept()
    {
        // 1) -- Backlot assign expression engine for calculating refering values.
        AssignExpressionEngineFor<string, MustachExpressionEngine>();

        // 2) -- Backlot instructors
        
        AssignInstructorFor<IFormula>(Instructors.AliasInitializer);
        AssignInstructorFor<IPerson>(Instructors.AliasInitializer);
        
        // Try to avoid calling the db by using the EncryptedPermissionInitialization. This allows clients to send a valid encrypted version of the permission.
        AssignInstructorFor<IPersist>(Core.Security.PermissionInitialization.EncryptedPermissionInitialization, 997);
        // Set DbAccessInitialization as default for for all roles that are persisted and are not already set by EncryptedPermissionInitialization.
        AssignInstructorFor<IPersist>(Core.Security.PermissionInitialization.DbAccessInitialization, 998);
        // When not persisted we set the default permission:
        AssignInstructorFor<IPermission>(Core.Security.PermissionInitialization.AllAccessInitialization, 999);
        
        // 3) -- Backlot prepare scenarios for playing
        
        PrepareCompositionFor<WhoAmI>(scenario =>
        {
            scenario.GetInfo = async () =>
            {
                var ur = ServiceLocator.Get<IUserRepository>();
                var user = await ur.TryGetUser(Core.Security.UserContext.Current.UserName);
                if (user.success)
                    return JObject.Parse(user.settings);
                
                return JObject.Parse(string.Empty);
            };
        });
        
        // -- here ...
        
        // 4) -- Backlot define watchers for all or per scenario.
        
        // Watch<Login, Services.Postmark.MailWatcher<Login>>();
        WatchAll<DebugWatcher>();
        Watch<Calculate, FollowUpScenarioWatcher<Calculate, FollowUp>>();
        
        // example when having a the authentication library installed which does have a login scenario, such as Authentication.Jwt
        Watch<Login, MailWatcher<Login>>(l =>
        {
            l.Events = nameof(Login.Authenticated); // comma seperated in case you want add extra events.
        });
    }
    
    
    public override void Registration()
    {
        base.Registration();
        
        #region Backlot.Authentication 
        
        // custom needs for authentication.
        
        Builder.Register(_ => new JwtTokenService($"token_security{SecretKey}"))
            .As<JwtTokenService>()
            .SingleInstance();
                
        Builder.RegisterType<UserFileRepository>()
            .As<IUserRepository>()
            .SingleInstance();
            
        Builder.RegisterType<DummyTokenRepository>()
            .As<ITokenRepository>()
            .SingleInstance(); // token repository is singleton.
        
        #endregion
        
        #region Backlot.Functions Media Formatters
        
        // custom media formatters.
        
        Builder.RegisterType<CsvFormatter>()
            .As<IMediaFormatter>()
            .InstancePerRequest();

        Builder.RegisterType<MustacheTemplateFormatter>()
            .As<IMediaFormatter>();
        
        #endregion
        
    }
}