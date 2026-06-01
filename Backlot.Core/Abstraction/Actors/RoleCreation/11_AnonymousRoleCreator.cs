using Backlot.Core.Json.Serialization.Newtonsoft;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Role creator where the actor is a dictionary based.
/// Anonymous are handled as they are "Jcontainers"
/// </summary>
public class AnonymousRoleCreator : JBaseRoleCreator
{
    public override int Priority => 11;
    
    protected override JContainer GetJContainer(object origin)
    {
        return JObject.FromObject(origin, Strategy.SerializeSafe);
    }
    
    public override bool CanCreate<TRole>(object origin)    
    {
        // do not call base!
        return origin.GetType().IsAnonymous();
    }

    protected override TRole Create<TRole>(JContainer origin)
    {
        return JsonInterceptor.Create<TRole>(origin);
    }
}