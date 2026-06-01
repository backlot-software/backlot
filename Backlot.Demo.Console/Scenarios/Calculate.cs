using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Demo.Console.Roles;

namespace Backlot.Demo.Console.Scenarios;

[Scenario(typeof(Calculate), access: [Access.Open])]
public class Calculate : Scenario<Calculate, IFormula, IResult>
{
    //[ExcludeValidation]
    private ICart Cart { get; set; }

    public Calculate(IFormula role, [NullAllowed] ICart cart, [NullAllowed] IMoney money, IPersistedRoleRepository roleRepository) : base(role)
    {
        Cart = cart;
    }

    protected override IResult Exec()
    {
        if (Role.Operation.Equals("sum", StringComparison.CurrentCultureIgnoreCase))
        {
            var result = new 
            {
                Uid = Guid.NewGuid().ToString(),
                Outcome = Role.Number1 + Role.Number2,
                Info = $"This result is calculated on {DateTime.Now.ToShortDateString()}"
            }.Presents<IResult>();

            result.ManagePermission(p => p
                .SetGroup(Access.Admin, PermissionLevel.ReadWrite)
                .SetGroup("CMSUsers", PermissionLevel.ReadWrite)
                .SetUser("John@doe.com", PermissionLevel.Read));
            
            return result;
        }
        else
        {
            throw new ArgumentException("Unknown Operator");
        }
    }
}