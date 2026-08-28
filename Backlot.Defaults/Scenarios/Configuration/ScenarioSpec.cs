using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Defaults.Scenarios.Configuration;

/// <summary>
/// The machine-readable contract for this deployment's endpoints, as TypeSpec source.
/// </summary>
/// <remarks>
/// Where <see cref="ScenarioSchemas"/> documents the API by example for a human in Backlot Studio,
/// this documents it by type for a code generator -- an example body cannot carry optionality, enum
/// members, integer width, or the element type of an empty array, which is exactly what a non-.NET
/// client needs.
///
/// Kept as its own scenario because it is a build-time artifact: a consumer downloads it once and
/// runs it through <c>tsp compile</c>, unlike the three Studio pages that play
/// <see cref="Scenarios"/> on every load.
/// </remarks>
[Scenario(typeof(ScenarioSpec), access: [Access.Everyone])]
public class ScenarioSpec : DirectorScenario<ScenarioSpec, string>
{
    public ScenarioSpec(IDirector role) : base(role)
    {
    }

    protected override async Task<string> ExecAsync()
    {
        // Scenarios already applies whatever access filtering it is configured for, so the emitted
        // contract covers exactly the endpoints this caller may play.
        var scenarios = (await Scenarios.Play()).ToList();

        return TypeSpecEmitter.Emit(scenarios, typeof(IDirector).GetRoleName());
    }
}
