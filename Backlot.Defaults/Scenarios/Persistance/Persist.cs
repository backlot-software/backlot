using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Defaults.Scenarios.Persistance;

[Scenario(typeof(Persist), access: [Access.Everyone])]
public class Persist : Scenario<Persist, IPersist, IPersist>
{
    public Persist(IPersist role) : base(role)
    {
    }

    protected override IPersist Exec()
    {
        // no need to call "RoleRepository.Persist" here because the scenario execution takes care of them itself.
        return Role; //because the returning Role is an IPersist it is saved during PersistAndRelate()..
    }
}

