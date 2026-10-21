using Backlot.Core.Abstraction.Scenarios;
using Backlot.Testing.Shared.TestModels;

namespace Backlot.Testing.Shared.Scenarios;

/// <summary>
/// Dummy scenario having 1 role and a none persistable Result
/// </summary>
public class Dummy : Scenario<Dummy, IFormula, object>
{
    public Dummy(IFormula role) : base(role)
    {

    }

    protected override object Exec()
    {
        return Role.Name;
    }
}