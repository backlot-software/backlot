using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Http;

public static class RoleExtensions
{
    /// <summary>
    /// Use Backlot.Core Play async, but check on authentication first.
    /// </summary>
    /// <param name="roles"></param>
    /// <param name="scenario"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public static async Task<object> PlayAuthAsync(
        this IRole[] roles,
        string scenario)
    {
        var scenarioReference = new ScenarioReference { Name = scenario };
        
        // AUTHENTICATION   --->
        var watch = (IScenario sc) =>
        {
            sc.Before += (s, _) =>
            {
                var scene = s as IScenario;
                if (scene == null) throw new ArgumentException("Unknown scene while authenticating.");

                if (scene.Info.Access.Contains("*")) // when this scenario is open for all users
                {
                    return Task.CompletedTask;
                }

                // check if userContext.current is in any of the roles in scene.info.access

                if (scene.Info.Access.Contains(UserContext.Current.UserName)) return Task.CompletedTask;

                if (UserContext.Current.IsAuthenticated)
                {
                    if(scene.Info.Access.Any(a => a == Access.Everyone)) return Task.CompletedTask;
                    
                    foreach (var userrole in scene.Info.Access)
                    {
                        if (UserContext.Current.IsInGroup(userrole))
                        {
                            return Task.CompletedTask;
                        }
                    }
                }

                throw new UnauthorizedAccessException($"Not authorized to execute scenario {scenario}");
            };
        };
        // <--- AUTHENTICATION

        return roles.Length > 1
            ? await scenarioReference.Play(roles, watch)
            : await scenarioReference.Play(roles.First(), watch);
    }
}