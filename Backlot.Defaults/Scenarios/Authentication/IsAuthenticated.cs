using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Defaults.Scenarios.Authentication;

[Scenario(typeof(IsAuthenticated), access: [Access.Everyone])]
public class IsAuthenticated : DirectorScenario<IsAuthenticated, bool>
{
    public IsAuthenticated(IDirector role) : base(role)
    {
    }

    protected override bool Exec()
    {
        return UserContext.Current.IsAuthenticated;
    }
}
