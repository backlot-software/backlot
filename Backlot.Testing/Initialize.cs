using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Core;
using Microsoft.Extensions.Logging;

namespace Backlot.Testing;

public static class Initialize
{
    
    public static void Setup(
        Func<ContainerBuilder, IDirector>? buildDirector = null,
        Action<ContainerBuilder>? registerRepos = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<UserCtx>().As<IUserContext>();

        //builder.RegisterType<RavenPersistedRoleRepository>().As<IPersistedRoleRepository>();
        //builder.RegisterType<RavenRelationRepository>().As<IRelationRepository>();

        if (registerRepos == null)
        {
            builder.RegisterType<MemoryPersistedRoleRepository>().As<IPersistedRoleRepository>();
            builder.RegisterType<MemoryRelationRepository>().As<IRelationRepository>();
            builder.Register(_ => new EncryptionService("1234567890ABCDEF"))
                .As<IEncryptionService>()
                .SingleInstance();
        }
        else
        {
            registerRepos(builder);
        }

        builder.Register(_ => new ChecksumBuilder(System.Security.Cryptography.MD5.Create().ComputeHash)).As<IChecksumBuilder>();

        var configstub = new ConfigStub(new Dictionary<string, string>
        {
            { "Backlot.Services.RavenDb.Settings.ServerUrl", "http://127.0.0.1:8080" },
            { "Backlot.Services.RavenDb.Settings.DatabaseName", "Development.Versla" },
            { "Backlot.Services.RavenDb.Settings.X509Certificate2", "" }
        });
        
        var director = buildDirector == null ? new Director(new LocalDiskStorage(), configstub, builder) : buildDirector(builder);
        
        builder.RegisterInstance(new LoggerFactory())
            .As<ILoggerFactory>();
        
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();
        
        // director.Registration(); is not called because we do registrations manually for unit-testing.
        director.Incept();
        
        // Building 

        var serviceProvider = new AutofacServiceProvider(builder.Build());
        ServiceLocator.Configure(serviceProvider);

        // Executing from builded provider
        
        ServiceLocator.Get<IPersistedRoleRepository>().FlushDb(); //each test starts with a clean database.
    }
}