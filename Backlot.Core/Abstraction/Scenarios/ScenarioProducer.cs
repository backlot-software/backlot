using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;

namespace Backlot.Core.Abstraction.Scenarios;

public class ScenarioProducer<TScenario, TRole, TResult>
    where TRole : IRole
    where TScenario : IScenario<TRole, TResult>
{
    private readonly List<IRole> _roles = [];

    public ScenarioProducer(IRole mainRole)
    {
        _roles.Add(mainRole);
    }

    /// <summary>
    /// Adds a supporting role to the scenario.
    /// </summary>
    public ScenarioProducer<TScenario, TRole, TResult> With<TSupportingRole>(TSupportingRole role)
        where TSupportingRole : IRole
    {
        _roles.Add(role);
        return this;
    }

    /// <summary>
    /// Adds an actor that will be presented as a specific role type.
    /// </summary>
    public ScenarioProducer<TScenario, TRole, TResult> With<TSupportingRole>(object actor)
        where TSupportingRole : IRole
    {
        _roles.Add(actor.Presents<TSupportingRole>());
        return this;
    }

    /// <summary>
    /// Executes the scenario with the collected roles.
    /// </summary>
    /// <param name="watch">optional watcher</param>
    /// <param name="named">used for named configurations while building the scenario</param>
    /// <returns>The result of the scenario</returns>
    public async Task<TResult> Play(Action<TScenario> watch = null, string named = null)
    {
        var scene = (TScenario)ScenarioBuilder.Construct(typeof(TScenario), _roles, named);
        watch?.Invoke(scene);
        
        await scene.Start();
        return scene.ResultValue;
    }
}