using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;

// ReSharper disable VirtualMemberNeverOverridden.Global
namespace Backlot.DependencyInjection.Autofac;

/// <summary>
/// Director including an autofac containerBuilder
/// </summary>
public abstract class AutofacContainerDirector : Director
{
    /// <summary>
    /// Director including an autofac containerBuilder
    /// </summary>
    protected AutofacContainerDirector(IFileSystem fileSystem,
        IConfigurationManager configurationManager,
        ContainerBuilder builder) : base(fileSystem, configurationManager)
    {
        Builder = builder;
        
        builder.Register(_ => configurationManager).As<IConfigurationManager>();
        builder.Register(_ => fileSystem).As<IFileSystem>();
        builder.Register(_ => this).As<IDirector>();
    }

    protected ContainerBuilder Builder { get; }

    protected override void WatchAll<TWith>(Action<TWith>? configure = null)
    {
        Builder.RegisterType<TWith>()
            .OnActivated(e =>
            {
                if (configure != null) configure(e.Instance);
                e.Context.Resolve<IConfigurationManager>().ResolveConfiguration(e.Instance); // configuration is always leading.
            })
            .As<IBingeWatcher>();
    }

    protected override void Watch<TScene, TWith>(Action<TWith>? configure = null)
    {
        // when the Watcher is a Generic watcher implementing a generic equal to the scene, than use the sene name as the "named" parameter selector for resolving the configuration.
        var named = typeof(TWith).GetGenericArguments().FirstOrDefault(x => x == typeof(TScene))?.Name;

        Builder.RegisterType<TWith>()
            .OnActivated(e =>
            {
                
                if (configure != null) configure(e.Instance);

                e.Context.Resolve<IConfigurationManager>().ResolveConfiguration(e.Instance,
                    named); // configuration is always leading.
            })
            .As<ISceneWatcher<TScene>>();
    }

    protected override void AssignInstructorFor<TRole>(Func<TRole, object, TRole> instructor)
    {
        Builder.Register(_ => new Instructor<TRole>(instructor));
    }

    protected override void AssignInstructorFor<TRole>(Func<TRole, object, TRole> instructor, int priority)
    {
        Builder.Register(_ => new Instructor<TRole>(instructor) { Priority = priority });
    }

    protected override void AssignExpressionEngineFor<T, TE>()
    {
        Builder.RegisterType<TE>()
            .As<IExpressionEngine<T>>()
            .SingleInstance();
    }

    protected override void PrepareCompositionFor<TScene>(Action<TScene> guide)
    {
        Builder.Register(_ => new Composer<TScene>(guide))
            .InstancePerDependency();
    }
}