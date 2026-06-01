using System;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Demo.Azure.Roles;

namespace Backlot.Demo.Azure.Scenarios;

[Scenario(typeof(Calculate), access: [Access.Open])]
public class Calculate : Scenario<IFormula, IResult>
{
    public Calculate(IFormula role) : base(role)
    {
        
    }

    protected override IResult Exec()
    {
        
        if (Role.Operation.Equals("sum", StringComparison.CurrentCultureIgnoreCase))
        {
            Role.Outcome = Role.Number1 + Role.Number2;
            
            return new 
            {
                Uid = Guid.NewGuid().ToString(),
                Outcome = Role.Number1 + Role.Number2,
                Info = $"This result is calculated on {DateTime.Now.ToShortDateString()}"
            }.Presents<IResult>();
        }
        else
        {
            throw new ArgumentException("Unknown Operator");
        }
    }
}