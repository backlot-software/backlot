using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Demo.Azure.Roles;

namespace Backlot.Demo.Azure.Scenarios;

[Scenario(typeof(ProcessPerson), access: [Access.Open])]
public class ProcessPerson  : Scenario<IPerson, IPerson>
{
    public ProcessPerson(IPerson role) : base(role)
    {
    }

    protected override IPerson Exec()
    {
        // do some processing.
        if (Role.Firstname != null || Role.Lastname != null)
        {
            Role.Fullname = $"{Role.Firstname} {Role.Lastname}";
        }
        return Role;
    }
}