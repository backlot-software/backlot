using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Defaults.Scenarios.Persistance;

/// <summary>
/// Scenario is using the 
/// </summary>
[Scenario(typeof(IsValid), access: [Access.Open])]
public class IsValid : Scenario<IsValid, IRole, object>
{

    public IsValid(IPersist role) : base(role)
    {
    }

    public override bool Validate()
    {
        return true; // validateion is always true in this case, because the scenario itself is giving validation information.
    }

    protected override object Exec()
    {
        var res = base.Validate();
        
        return new
        {
            IsValid = res,
            Results = ValidationResults
        };
    }
}

