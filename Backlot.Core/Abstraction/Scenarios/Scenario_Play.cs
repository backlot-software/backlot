using System;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
// ReSharper disable MemberCanBeProtected.Global : Used by consumers of this libarry.

namespace Backlot.Core.Abstraction.Scenarios;

/// <summary>
/// Enable .Play(role) to build and play this scenario.
/// You can execute these scenarios as if static functions with parameters.
/// </summary>
/// <typeparam name="TScenario">The scenario itself</typeparam>
/// <typeparam name="TRole">The main role responsible for playing this scenario. If you don't have any use the IDirector</typeparam>
/// <typeparam name="TResult">The result the implementation of Exec will return</typeparam>
public abstract class Scenario<TScenario, TRole, TResult> : Scenario<TRole, TResult>
    where TRole : IRole
    where TScenario : IScenario<TRole, TResult>
{
    /// <summary>
    /// DEFAULT: Initialized automatically
    /// </summary>
    /// <param name="role"></param>
    /// <param name="named"></param>
    /// <exception cref="ArgumentException"></exception>
    protected Scenario(TRole role, string named=null) : base(role, named)
    {
    }

    /// <summary>
    /// Play the scenario using the typed <see cref="TRole" />
    /// </summary>
    /// <param name="role">The object presented as <see cref="TRole" /></param>
    /// <param name="watch">optional watcher</param>
    /// <param name="named">used for named configurations while building the scenario</param>
    /// <returns>The result of the scenario</returns>
    public static async Task<TResult> Play(TRole role, Action<TScenario> watch = null, string named=null)
    {
        // ScenarioBuilder automatically resolves other constructor parameters from IOC
        var scene = (TScenario)ScenarioBuilder.Construct(typeof(TScenario), role, named);
        watch?.Invoke(scene);
        await scene.Start();
        return scene.ResultValue;
    }

    /// <summary>
    /// Play the scenario using an actor. The actor will automatically be presented as <see cref="TRole" />
    /// </summary>
    /// <param name="actor">An actor not yet defined as the role</param>
    /// <param name="watch">optional watcher</param>
    /// <param name="named">used for named configurations while building the scenario</param>
    /// <returns>The result of the scenario</returns>
    public static async Task<TResult> Play(object actor, Action<TScenario> watch = null, string named=null)
    {
        // ScenarioBuilder automatically resolves other constructor parameters from IOC
        var scene = (TScenario)ScenarioBuilder.Construct(typeof(TScenario), actor.PresentsType(typeof(TRole)), named);
        watch?.Invoke(scene);
        
        await scene.Start();
        return scene.ResultValue;
    } 
    
    /// <summary>
    /// Starts a fluent builder using an actor will be presented as the main role.
    /// You need to finish scenario producer with .Play()
    /// </summary>
    public static ScenarioProducer<TScenario, TRole, TResult> With(object actor)
    {
        return new ScenarioProducer<TScenario, TRole, TResult>(actor.Presents<TRole>());
    }
    
    /// <summary>
    /// Starts a fluent builder using the main role
    /// You need to finish scenario producer with .Play()
    /// </summary>
    public static ScenarioProducer<TScenario, TRole, TResult> With(TRole role)
    {
        return new ScenarioProducer<TScenario, TRole, TResult>(role);
    }
    
}

/// <summary>
/// Director scenarios do not require any roles. The singleton director is the role it self.
/// You can execute these scenarios as if they are parameter less static functions using .Play().
/// </summary>
/// <typeparam name="TScenario"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class DirectorScenario<TScenario, TResult> : Scenario<IDirector, TResult>
    where TScenario : IScenario<IDirector, TResult>
{
    /// <summary>
    /// DEFAULT: Initialized automatically
    /// </summary>
    /// <param name="role"></param>
    /// <param name="named"></param>
    /// <exception cref="ArgumentException"></exception>
    protected DirectorScenario(IDirector role, string named=null) : base(role, named)
    {
    }
    
    /// <summary>
    /// Play a scenario without parameters using its director
    /// </summary>
    /// <param name="watch">optional watcher</param>
    /// <param name="named">used for named configurations while building the scenario</param>
    /// <returns>The result of the scenario</returns>
    public static async Task<TResult> Play(Action<TScenario> watch = null, string named=null)
    {
        // ScenarioBuilder automatically resolves other constructor parameters from IOC
        var scene = (TScenario)ScenarioBuilder.Construct(typeof(TScenario), ServiceLocator.Get<IDirector>(), named);
        watch?.Invoke(scene);
        
        await scene.Start();
        return scene.ResultValue;
    }
}