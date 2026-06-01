using System;
using System.Collections.Generic;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Role creator where the actor is a dictionary based.
/// </summary>
public class DictionaryRoleCreator : IRoleCreator
{
    public int Priority => 10;
    
    public bool CanCreate<TRole>(object origin) where TRole : IRole
    {
        return origin is IDictionary<string, object>;
    }

    public TRole Create<TRole>(object origin, bool checkCanCreate=true) where TRole : IRole
    {
        if (checkCanCreate && !CanCreate<TRole>((origin)))
        {
            throw new ArgumentException($"This role creator {nameof(DictionaryRoleCreator)} cannot create a role by using the given {nameof(origin)}. Please use {nameof(CanCreate)} to avoid this exception.");
        }

        var actor = origin as IDictionary<string, object>;
        return DictionaryInterceptor.Generate<TRole>(actor);
    }
}