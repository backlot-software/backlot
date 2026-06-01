using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;

namespace Backlot.Defaults.Scenarios.Persistance;

[Scenario(typeof(Detail), access: [Access.Everyone])]
public class Detail : Scenario<Detail, ISeekBase, object>
{
    private readonly IRelationRepository _relationRepository;
    private readonly IPersistedRoleRepository _roleRepository;

    protected override bool PersistAndRelate => false; // no persistance allowed within this scenario.

    public Detail(ISeekBase role, 
        IRelationRepository relationRepository, IPersistedRoleRepository roleRepository) : base(role)
    {
        _relationRepository = relationRepository;
        _roleRepository = roleRepository;
    }

    protected override async Task<object> ExecAsync()
    {
        if (_roleRepository.TryGet<IPersist>(Role.For.Uid, out var persisted))
        {
            // todo: only 999 relations can be returned more is not possible right now.
            var references = _relationRepository.GetAll(Role.For);
            var relations = await _roleRepository.GetBulk(references);

            return new
            {
                Role = persisted,
                Relations = relations,
            };
        }

        return new
        {
            Role = Acting.New<IRole>(),
            Relations = Enumerable.Empty<IPersist>(),
        };
    }
    
    // not need to override PersistAndRelate, because default Backlot only saves relations and objects when they are defined as IPersist from the scenario requirements.
}

