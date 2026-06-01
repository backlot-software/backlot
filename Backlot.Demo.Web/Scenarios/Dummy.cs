using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Demo.Web.Roles;

namespace Backlot.Demo.Web.Scenarios;

[Scenario(typeof(Dummy), access: [Access.Everyone])]
public class Dummy : Scenario<Dummy, IPersist, string>
{
    public Dummy(IPersist role, IFormula formula) : base(role)
    {

    }

    protected override string Exec()
    {
        return Role.Name;
    }
}