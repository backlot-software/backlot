using System;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Selfs
/// </summary>
public class SelfRoleCreator : IRoleCreator
{
    public int Priority => 1;
    public bool CanCreate<TRole>(object origin) where TRole : IRole
    {
        return origin is TRole;
    }

    public TRole Create<TRole>(object origin, bool checkCanCreate = true) where TRole : IRole
    {
        if(checkCanCreate && !CanCreate<TRole>(origin))
            throw new ArgumentException($"This role creator {nameof(SelfRoleCreator)} cannot create a role by using the given {nameof(origin)}. Please use {nameof(CanCreate)} to avoid this exception.");

        return (TRole)origin;
    }
}