using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Scheduling.Roles;
using Backlot.Scheduling.Scenarios;

namespace Backlot.Demo.Web.Scenarios;

public interface IHeart : IJob
{
    int Beats { get; set; }
}

/// <summary>
/// Played by the scheduler.
/// Also playable manually by that user via GET /api/role/heart/beat?uid= ...
/// </summary>
[Scenario(typeof(Beat), access: [Access.Admin])] // or user the name of the SuperAdmin at Backlot.Scheduling
public class Beat(IHeart role) : JobScenario<IHeart>(role)
{
    protected override bool Exec()
    {
        Role.Beats += 1; // Increment the Beats with one beat
        return true;
    }
}
