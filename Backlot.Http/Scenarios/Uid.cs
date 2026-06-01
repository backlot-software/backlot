using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Http.Scenarios;

[Scenario(typeof(Uid), access: [Access.Open])]
public class Uid(IDirector director) : DirectorScenario<Uid, string>(director)
{
    protected override string Exec()
    {
        return Core.Uid.New();
    }
}

