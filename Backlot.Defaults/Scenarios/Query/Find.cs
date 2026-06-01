using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;

namespace Backlot.Defaults.Scenarios.Query;

[Scenario(typeof(Find), access: [Access.Everyone])]
public class Find(ISimpleQuery role, IPersistedRoleRepository roleRepository)
    : Scenario<Find, ISimpleQuery, PagedResultCollection<IRole>>(role)
{
    protected override bool PersistAndRelate => false; // no persistance allowed within this scenario.

    protected override PagedResultCollection<IRole> Exec()
    {
        var roleType = Loader.GetRoleByName(Role.For);
        return new PagedResultCollection<IRole>()
        {
            Results = roleRepository
                .GetAll(roleType, 
                    Role.Page, 
                    Role.PageSize < 1 ? 999 : Role.PageSize, 
                    out var total, 
                    Role.Criteria,
                    from: Role.From ?? DateTimeOffset.MinValue,
                    till: Role.Till ?? DateTimeOffset.Now,
                    orderby: Role.OrderBy)
                .OfType<IPersist>(),
            Total = total,
            PageSize = Role.PageSize,
            Page = Role.Page
        };
    }
}

