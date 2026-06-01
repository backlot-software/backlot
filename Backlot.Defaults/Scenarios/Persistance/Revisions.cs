using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;

namespace Backlot.Defaults.Scenarios.Persistance;

[Scenario(typeof(Revisions), access: [Access.Admin])]
public class Revisions : Scenario<Revisions, ISeekBase, IEnumerable<Revision>>
{
    private readonly IPersistedRoleRepository _repo;

    protected override bool PersistAndRelate => false; // no persistance allowed within this scenario.

    public Revisions(ISeekBase role,
        IPersistedRoleRepository repo) : base(role)
    {
        _repo = repo;
    }

    protected override IEnumerable<Revision> Exec()
    {
        return _repo.GetRevisions<IPersist>(Role.For.Uid);
    }
}