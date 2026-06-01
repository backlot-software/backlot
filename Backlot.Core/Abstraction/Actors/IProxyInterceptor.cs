using Castle.DynamicProxy;

namespace Backlot.Core.Abstraction.Actors;

/// <summary>
/// INTNERAL: The interceptor that is used to intercept the calls to the actor.
/// Advanced usage only, and advisable to not use in none core libraries.
/// -- Interface and use be changed without any notice.
/// -- When implement a new interceptor, make sure to implement it via this interface and not directly via IInterceptor.
/// </summary>
public interface IProxyInterceptor : IInterceptor
{
    /// <summary>
    /// Combines two actors into one.
    /// The result is the actor used by this interceptor.
    /// The already existing actor is leading.
    /// </summary>
    /// <param name="additional"></param>
    void CombineActor(IProxiedRole additional);

    /// <summary>
    /// A dynamic representation of the current actor properties and values.
    /// Key: alias used by the actor
    /// Value: the value
    /// </summary>
    /// <returns></returns>
    string[] ActorProperties();
    
    /// <summary>
    /// Get the value of an ACTOR property
    /// </summary>
    /// <param name="alias">The alias name used by the actor, 90% of the time this is the propertyname of the role as well.</param>
    /// <param name="value">The actual value which needs to be returned.</param>
    /// <returns>True when the alias is represented by the actor, false if not</returns>
    bool TryGet(string alias, out object value);
    
}