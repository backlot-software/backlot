using Backlot.Core.Json.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Role creator for when the origin it is typed, but not an IRole itself.
/// </summary>
public class TypedObjectRoleCreator: JBaseRoleCreator
{
    public override int Priority => 12;
    
    protected override JContainer GetJContainer(object origin)
    {
        var ser = Strategy.SerializeSafe;
        ser.NullValueHandling = NullValueHandling.Include; // we want to include null values in the json for typed objects. When an object is typed these values are set explicitly to null and therefor we need to take them into account.
        
        // we handle these objects the same as anonymous objects.
        return JObject.FromObject(origin, ser);
    }
    
    public override bool CanCreate<TRole>(object origin)    
    {
        // do not call base!
        return !(origin is string) && !(origin is JToken) && !origin.GetType().IsValueType && origin is not IRole;
    }

    protected override TRole Create<TRole>(JContainer origin)
    {
        return JsonInterceptor.Create<TRole>(origin);
    }
}