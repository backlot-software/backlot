using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Core.Abstraction.Roles;

/// <summary>
/// Role extensions to play scenarios
/// </summary>
public static class RolePlayExtensions
{
        [Obsolete("Use ScenarioReference.Play instead. This will be removed in 1.0.0")]
        public static async Task<TResult> PlayAsync<TRole, TResult>(this TRole role, Func<TRole, TResult> func, Action<FuncScenario<TRole, TResult>> watch = null)
            where TRole : IRole
        {
            var scene = new FuncScenario<TRole, TResult>(role, func);
            watch?.Invoke(scene); 
            await scene.Start();
        
            return scene.ResultValue;
        }
        
        [Obsolete("Use ScenarioReference.Play instead. This will be removed in 1.0.0")]
        public static async Task<TResult> PlayAsync<TResult>(this IRole role, ScenarioReference scenario, Action<IScenario> watch = null)
        {
            var result = await PlayAsync(role, scenario, watch);
            if (result is TResult typedResult)
                return typedResult;
        
            throw new ArgumentException(
                $"Scenario '{scenario.Name}' for role '{role.GetType().Name}' does not return expected '{typeof(TResult).Name}'");
        }
        
        [Obsolete("Use ScenarioReference.Play instead. This will be removed in 1.0.0")]
        public static async Task<object> PlayAsync(this IRole role, ScenarioReference scenario, Action<IScenario> watch = null)
        {
            var scene = ScenarioBuilder.Construct(scenario, role);
            watch?.Invoke(scene);
            await scene.Start();
        
            return scene.ResultValue;
        }
        
        [Obsolete("Use ScenarioReference.Play instead. This will be removed in 1.0.0")]
        public static async Task<object> PlayAsync(this IEnumerable<IRole> roles, ScenarioReference scenario, Action<IScenario> watch = null)
        {
            var scene = ScenarioBuilder.Construct(scenario, roles);
            watch?.Invoke(scene);
            await scene.Start();
        
            return scene.ResultValue;
        }
        
        [Obsolete("Use Scenario.Play instead. This will be removed in 1.0.0")]
        public static async Task<TResult> PlayAsync<TScenario,TResult>(this IRole role, Action<TScenario> watch = null, string named=null)
            where TScenario : IScenario<IRole, TResult>, IScenario
        {
            return (TResult) await role.PlayAsync(watch, named);
        }
        
        [Obsolete("Use Scenario.Play instead. This will be removed in 1.0.0")]
        public static async Task<object> PlayAsync<TScenario>(this IRole role, Action<TScenario> watch = null, string named=null)
            where TScenario : IScenario
        {
            var scene = (TScenario)ScenarioBuilder.Construct(typeof(TScenario), role, named);
            watch?.Invoke(scene);
            
            await scene.Start();
            return scene.ResultValue;
        }
        
        [Obsolete("Use Scenario.Play instead. This will be removed in 1.0.0")]
        public static async Task<(bool IsSuccess, TResult Result)> PlayWhenValidAsync<TResult>(this IRole role, ScenarioReference scenario, Action<IScenario> watch = null)
        {
            var paw = await PlayWhenValidAsync(role, scenario, watch);
        
            if (paw is { IsSuccess: true, Result: TResult typedResult })
            {
                return (true, typedResult);
            }
            
            return (paw.IsSuccess, default);
        }
        
        [Obsolete("Use Scenario.Play instead. This will be removed in 1.0.0")]
        public static async Task<(bool IsSuccess, object Result)> PlayWhenValidAsync(this IRole role, ScenarioReference scenario, Action<IScenario> watch = null)
        {
            var scene = ScenarioBuilder.Construct(scenario, role);
        
            if (scene.Validate())
            {
                watch?.Invoke(scene);
                await scene.Start();
        
                return (true, scene.ResultValue);
            }
        
            return (false, null);
        }
        
        [Obsolete("Use Scenario.Play instead. This will be removed in 1.0.0")]
        public static async Task<object> PlayAsync<TScenario>(this IEnumerable<IRole> roles, Action<IScenario> watch = null, string named=null)
            where TScenario: IScenario
        {
            var scene = ScenarioBuilder.Construct(typeof(TScenario), roles, named);
            watch?.Invoke(scene);
            await scene.Start();
        
            return scene.ResultValue;
        }
}