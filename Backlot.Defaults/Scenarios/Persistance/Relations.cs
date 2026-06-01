using Backlot.Core;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.Defaults.Scenarios.Persistance;

[Scenario(typeof(Relations), access: [Access.Everyone])]
public class Relations : Scenario<Relations, IPersist, IEnumerable<RoleReference>>
{
    private readonly IRelationRepository _relationRepository;

    protected override bool PersistAndRelate => false;

    public Relations(IPersist role,
        IRelationRepository relationRepository) : base(role)
    {
        _relationRepository = relationRepository;
    }

    protected override IEnumerable<RoleReference> Exec()
    {
        return _relationRepository.GetAll(Role.GetReference());
    }
}

