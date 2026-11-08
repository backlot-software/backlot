using System;
using System.Linq;
using System.Reflection;
using Backlot.Core.Exceptions;

namespace Backlot.Core.Abstraction.Scenarios;

/// <summary>
/// Generic ScenarioReference extensions
/// </summary>
public static class ScenarioReferenceExtensions
{
    extension(ScenarioReference scenario)
    {

        /// <summary>
        /// The scenario information this reference refers to.
        /// The named configuration part ("{scenarioname}.{configurationpath}") is ignored.
        /// </summary>
        /// <exception cref="NotFoundException">No scenario with this name exists.</exception>
        /// <exception cref="AmbiguousMatchException">No support yet for scenarios sharing the same role here. Multiple scenarios (for different roles) share this name and therefor a AmbiguousMatchException is thrown.</exception>
        public IScenarioInfo Info()
        {
            var sceneName = scenario.Name.Split(".")[0];

            var infos = Loader.GetAllScenarios()
                .Where(i => i.Name.Equals(sceneName, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

            return infos.Length switch
            {
                1 => infos[0],
                0 => throw new NotFoundException($"Scenario '{scenario.Name}' not found."),
                _ => throw new AmbiguousMatchException(
                    $"Scenario '{scenario.Name}' is ambiguous, it exists for roles: " +
                    $"{string.Join(", ", infos.Select(i => i.TRole.GetRoleName()))}.")
            };
        }
    }
}