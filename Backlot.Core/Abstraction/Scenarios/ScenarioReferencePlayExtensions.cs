using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// ReSharper disable MemberCanBePrivate.Global

namespace Backlot.Core.Abstraction.Scenarios;

/// <summary>
/// Play scenarios using their references
/// </summary>
public static class ScenarioReferencePlayExtensions
{
    extension(ScenarioReference scenario)
    {
        public async Task<TResult> Play<TResult>(IRole role, Action<IScenario> watch = null)
        {
            var result = await scenario.Play(role, watch);
            if (result is TResult typedResult)
                return typedResult;

            throw new ArgumentException(
                $"Scenario '{scenario.Name}' for role '{role.GetType().Name}' does not return expected '{typeof(TResult).Name}'");
        }

        public async Task<TResult> Play<TResult>(IEnumerable<IRole> roles, Action<IScenario> watch = null)
        {
            var result = await scenario.Play(roles, watch);
            if (result is TResult typedResult)
                return typedResult;

            throw new ArgumentException(
                $"Scenario '{scenario.Name}' for role '{nameof(IEnumerable<IRole>)}' does not return expected '{typeof(TResult).Name}'");
        }

        public async Task<object> Play(IRole role, Action<IScenario> watch = null)
        {
            var scene = ScenarioBuilder.Construct(scenario, role);
            watch?.Invoke(scene);
            await scene.Start();

            return scene.ResultValue;
        }

        public async Task<object> Play(IEnumerable<IRole> roles, Action<IScenario> watch = null)
        {
            var scene = ScenarioBuilder.Construct(scenario, roles);
            watch?.Invoke(scene);
            await scene.Start();

            return scene.ResultValue;
        }
    }
}