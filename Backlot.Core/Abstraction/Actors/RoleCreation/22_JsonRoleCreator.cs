using Backlot.Core.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Role creator where the actor is json based.
/// Does use Newtonsoft Json for creating the roles.
/// </summary>
public class JsonRoleCreator : JBaseRoleCreator
{
    public override int Priority => 22;

    protected override JContainer GetJContainer(object origin)
    {
        if(origin is JContainer jContainer) return jContainer;
        return JObject.Parse(origin as string ?? string.Empty);
    }

    public override bool CanCreate<TRole>(object origin)
    {
        if ((origin is string jsn && jsn.IsJson()) || origin is JContainer)
        {
            return typeof(TRole).IsInterface;
        }

        return false;
    }

    protected override TRole Create<TRole>(JContainer jActor)
    {
        return JsonInterceptor.Create<TRole>(jActor);
    }
}