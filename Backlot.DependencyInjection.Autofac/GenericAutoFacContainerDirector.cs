using Autofac;
using Autofac.Builder;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.DependencyInjection.Autofac;

public abstract class AutofacContainerDirector<TR, TP, TU, TC, TF>
    (IFileSystem fileSystem, IConfigurationManager configurationManager, ContainerBuilder builder) : 
    AutofacContainerDirector(fileSystem, configurationManager, builder)
    
    where TR : IRelationRepository
    where TP : IPersistedRoleRepository
    where TU : IUnitOfWork
    where TC : IUserContext
    where TF : ICacheFactory
{
    protected abstract string SecretKey { get; }

    public override void Registration()
    {
        Builder.RegisterType<TF>()
            .As<ICacheFactory>()
            .SingleInstance();
        
        Builder.Register(_ => new EncryptionService(SecretKey))
            .As<IEncryptionService>()
            .SingleInstance();
        
        Builder.RegisterType<TC>()
            .As<IUserContext>().InstancePerLifetimeScope();

        Builder.Register(_ => new ChecksumBuilder(System.Security.Cryptography.MD5.Create().ComputeHash))
            .As<IChecksumBuilder>()
            .SingleInstance();

        // 2.0) -- Database configuration

        Builder.RegisterType<TU>()
            .As<IUnitOfWork>().InstancePerLifetimeScope();

        Builder.RegisterType<TP>()
            .As<IPersistedRoleRepository>().InstancePerLifetimeScope();

        Builder.RegisterType<TR>()
            .As<IRelationRepository>().InstancePerLifetimeScope();
    }

}