using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Defaults.Roles;

namespace Backlot.Defaults.Scenarios.Persistance;

[Scenario(typeof(Relate), access: [Access.Admin])]
public class Relate : Scenario<Relate, IReferenceCollection, IEnumerable<Relation>>
{
    protected override bool PersistAndRelate => false; // we manage relations ourselves here, not needed, but just to make sure it is never going to do something else.
    
    
    public Relate(IReferenceCollection role) : base(role)
    {
    }

    protected override async Task<IEnumerable<Relation>> ExecAsync()
    { 
        return await EnsureRelations(Role.References);
    }
}