using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Scenarios.Configuration.Models;
// ReSharper disable MemberCanBePrivate.Global

namespace Backlot.Defaults.Scenarios.Configuration;

[Scenario(typeof(Scenarios), access: [Access.Everyone])]
public class Scenarios : DirectorScenario<Scenarios, IEnumerable<ScenarioResultItem>>
{
    private readonly IConfigurationManager _configurationManager;

    /// <summary>
    /// Configuration to hide scenarios based on current user context.
    /// Default is false which means all scenarios are returned.
    /// No matter what, the scenarios are not executable able for users without access.
    /// </summary>
    [Configurable]
    public bool RespectAccessRestrictions { get; set; } = false;
    
    public Scenarios(IDirector role, IConfigurationManager configurationManager) : base(role)
    {
        _configurationManager = configurationManager;
    }

    protected override IEnumerable<ScenarioResultItem> Exec()
    {
        var grouped = RespectAccessRestrictions ? 
            Loader.GetAllScenarios()
            // only the scenarios the user has access to.
            .Where(scene => scene.Access.Contains(Access.Open) || 
                            scene.Access.Contains(UserContext.Current.UserName) || 
                            scene.Access.Any(UserContext.Current.IsInGroup))
            .GroupBy(s => s.Name) :
            Loader.GetAllScenarios().GroupBy(s => s.Name); // when not respecting access restrictions, all scenarios are returned.
        
        var r = grouped.Select(i =>
        {
            var itm = i.First();

            var roles = TRoles(itm).Select(r => r.GetRoleName()).ToArray();

            var endpoints = new List<string>(); // the order of the scenarios is important

            if (roles.Length > 1) // when multiple roles are used you can execute this as a director scenario, then this is the most important one to document.
            {
                endpoints.Add($"/api/role/director/{i.Key}".ToLower());
            }
            
            // then the default role name is used; keep in mind that you then can not give other roles within the body.
            endpoints.Add($"/api/role/{itm.TRole.GetRoleName()}/{i.Key}".ToLower());
            //todo: then add all other synonyms for this scenario, which is every role that also has TRole as skill.
            
            return new ScenarioResultItem
            {
                Scenario = i.Key,
                Result = itm.TResult.FriendlyName(),
                ResultType = itm.TResult,
                Roles = roles,
                Tags = itm.Tags,
                // Endpoints when using Backlot.Http
                Endpoints = endpoints.ToArray(),
                // Does return the so called "named" configuration overloads.
                // Scenario references do support named configurations this property returns
                // only the names which are already configurated, when nothing is configurated, an empty array is returned.
                Configurations = _configurationManager.GetNames(itm.ConfigurationPath)
            };
        });

        return r;
    }
    
    
    // ReSharper disable once InconsistentNaming : Inline with TRole of the interface IScenarioInfo
    /// <summary>
    /// Get all roles used by the given scenario.
    /// The TResult of the scenario is excluded from this list and part the the IScenarioInfo itself.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static IEnumerable<Type> TRoles (IScenarioInfo info)
    {
        // todo: this code is based on (partly duplicated): 'internal static IScenario Construct(Type scenarioType, IEnumerable<IRole> input, string named)' in ScenarioBuilder.cs and therefor a candidate for refactoring.
        
        var scene = Loader.GetScenario(info);

        var parameters = scene switch
        {
            Type scenarioType => scenarioType
                .GetConstructors()
                .MaxBy(c => c.GetParameters().Length) //always select the constructor with the most parameters.
                ?.GetParameters(),
            MethodInfo method => method.GetParameters(),
            _ => null
        };
        
        return parameters == null ? [] :  
            parameters.Where(par => Loader
                .AllRoles
                .Any(lr => par.ParameterType.IsAssignableFrom(lr))
            ).Select(p => p.ParameterType);
    }
}