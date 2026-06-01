namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// RoleCreators do define how a role is build based on certain origins (not always the same as actor) types.
/// AWARE THREAD-SAFETY: Make sure implementations of these classes can act as static singletons.
/// Each single IRoleCreator is created once, but is used for every single actor.
/// </summary>
public interface IRoleCreator
{
    /// <summary>
    /// When more creators return true on CanCreate, the lowest prioritized will be used first.
    /// We advice to use a priority between 99 and 1000 for custom IRoleCreator implementations.
    /// All .Core IRoleCreators have a priority between 9 and 100. 1 is used for objects which already are a role
    /// Lower number is higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Can this creator create roles using the given origin.
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    bool CanCreate<TRole>(object origin)
        where TRole : IRole;

    /// <summary>
    /// Create the Role
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="checkCanCreate">Convention which can be implemented on create function to avoid to avoid an extra CanCreate check at execution Create.</param>
    /// <typeparam name="TRole"></typeparam>
    /// <returns></returns>
    TRole Create<TRole>(object origin, bool checkCanCreate=true) where TRole : IRole;
}