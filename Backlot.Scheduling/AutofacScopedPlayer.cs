using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Scheduling.Roles;
using Backlot.Scheduling.Scenarios;
using Backlot.Scheduling.Security;

namespace Backlot.Scheduling;

/// <summary>
/// Executes the <see cref="JobScenario"/> scenario for the given scheduler, within its own Aufofac scope.
/// </summary>
public class AutofacScopedPlayer : IScopedPlayer
{
    private static AutofacServiceProvider BeginScope (IUserContext user)
    {
        var context = ServiceLocator.Current;
        if (context is not AutofacServiceProvider parent)
            throw new ArgumentException(
                $"{nameof(AutofacScopedPlayer)} only supports; {nameof(AutofacServiceProvider)} to be the {nameof(ServiceLocator)}.");
        
        var scope = parent.LifetimeScope.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag,
            builder => builder.RegisterInstance(user).As<IUserContext>());
        var provider = new AutofacServiceProvider(scope);
        ServiceLocator.Configure(provider);
        return provider;
    }
    
    /// <summary>
    /// Executes the <see cref="JobScenario"/> scenario for the given scheduler, within its own Aufofac scope.
    /// </summary>
    public async Task PlayScoped(IJob job, TimeProvider time, CancellationToken cancellationToken)
    {
        await using var provider = BeginScope(new SchedulersUserContext());
        var uow = ServiceLocator.Get<IUnitOfWork>();
        var now = time.GetUtcNow();
        
        if (job.NextRun < now)
        {
            var role = job.Scenario.Info().TRole;
            var succeeded = await job.Scenario.Play<bool>(job.PresentsType(role), t => // run scheduler
            {
                if (t is IJobScenario scene)
                {
                    t.Before += (_, _) => // define parameters for this run.
                    {
                        scene.Time = time;
                        return Task.CompletedTask;
                    };
                }
            });
            
            if (!succeeded)
                return; // nothing is committed, so partial changes of the played scenario are rolled back.

            

            await uow.Commit();
        }
    }
}