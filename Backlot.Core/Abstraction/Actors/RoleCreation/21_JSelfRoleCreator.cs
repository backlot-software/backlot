using System;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Abstraction.Actors.RoleCreation;

/// <summary>
/// Creates _self roles represented by Json
/// But where the origin is represented by serialized json.
/// </summary>
public class JSelfRoleCreator : JBaseRoleCreator
{
    public override int Priority => 21;

    protected override JContainer GetJContainer(object origin)
    {
        return origin switch
        {
            string jsn when jsn.IsJson() => JObject.Parse(jsn),
            JContainer jContainer => jContainer,
            _ => null
        };
    }

    private static Type GetSelfType<TRole>(JToken construct) where TRole : IRole
    {
        if (!typeof(TRole).IsInterface)
            return typeof(TRole);
     
        if (construct == null) return null;
        
        var selfType = Type.GetType(construct.Value<string>());

        if (selfType != null && typeof(TRole).IsAssignableFrom(selfType))
        {
            return selfType;
        }

        return null;
    }

    public override bool CanCreate<TRole>(object origin)
    {
        var jc = GetJContainer(origin);
        if (jc == null) return false;

        // We can create when a construct is defined.
        var construct = jc[Meta.__Construct];

        if (construct != null)
        {
            var selfType = GetSelfType<TRole>(construct);

            if (selfType != null && typeof(TRole).IsAssignableFrom(selfType))
            {
                return true;
            }
        }

        return !typeof(TRole).IsInterface; // When TRole is not an interface we create the typed role based on that type.
    }

    protected override TRole Create<TRole>(JContainer origin)
    {
        var selfType = GetSelfType<TRole>(origin[Meta.__Construct]);
        try
        {
            var role = (TRole)origin.ToObject(selfType, Strategy.DeSerializeDefault);

            if (role == null)
                throw new InvalidOperationException(
                    $"Could not create role from serialized json, because the type does not implement {nameof(TRole)}");

            return role;
        }
        catch (JsonSerializationException jsEx)
        {
            throw new InvalidOperationException($"Can not create _selfRole {selfType.FullName} from serialized json. Probalby you have Interfaced properties within your selfrole. Try to use the IRole interface or change your propertytypes. Full serialization message: {jsEx.Message}", jsEx);
        }
    }
}