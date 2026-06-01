using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Testing.UseCases.NullAllowed.Roles;

namespace Backlot.Testing.UseCases.NullAllowed.Scenarios;

[Scenario(typeof(ProcessWithNullAllowed), access: [Access.Open])]
public class ProcessWithNullAllowed : Scenario<ProcessWithNullAllowed, ICustomerCard, bool>
{
    private readonly ICardPerson _cart;


    public ProcessWithNullAllowed(ICustomerCard role, [NullAllowed] ICardPerson cart) : base(role)
    {
        _cart = cart;
    }

    protected override bool Exec()
    {
        return true;
    }
}

[Scenario(typeof(ProcessWithoutNullAllowed), access: [Access.Open])]
public class ProcessWithoutNullAllowed : Scenario<ProcessWithoutNullAllowed, ICustomerCard, bool>
{
    private readonly ICardPerson _cart;


    public ProcessWithoutNullAllowed(ICustomerCard role, ICardPerson cart) : base(role)
    {
        _cart = cart;
    }

    protected override bool Exec()
    {
        return true;
    }
}

[Scenario(typeof(ProcessWithoutNullAllowedPersistedRelation), access: [Access.Open])]
public class ProcessWithoutNullAllowedPersistedRelation : Scenario<ProcessWithoutNullAllowedPersistedRelation, ICustomerCard, bool>
{
    private readonly ICardPerson _cart;


    public ProcessWithoutNullAllowedPersistedRelation(ICustomerCard role, IPersistedCardPerson cart) : base(role)
    {
        _cart = cart;
    }

    protected override bool Exec()
    {
        return true;
    }
}
