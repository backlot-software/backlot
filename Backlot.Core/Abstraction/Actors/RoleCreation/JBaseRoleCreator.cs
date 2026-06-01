using System;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Base origin for all actors represented by json (strings as well as JTokens)
/// </summary>
public abstract class JBaseRoleCreator : IRoleCreator
{
    public abstract int Priority { get; }

    protected abstract JContainer GetJContainer(object origin);

    public abstract bool CanCreate<TRole>(object origin) where TRole : IRole; // allow to override

    public TRole Create<TRole>(object origin, bool checkCanCreate = true) where TRole : IRole // not allowed to override
    {
        if (checkCanCreate && !CanCreate<TRole>(origin)) 
            throw new ArgumentException(
                $"This role creator {GetType().Name} cannot create a role by using the given actor. Please use {nameof(CanCreate)} to avoid this exception.");
        
        return Create<TRole>(GetJContainer(origin));
    }

    protected abstract TRole Create<TRole>(JContainer origin) where TRole : IRole; // must be implemented by the inheriting classes.
    
}