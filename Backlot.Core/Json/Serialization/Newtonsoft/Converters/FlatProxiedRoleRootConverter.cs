using System;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters;

/// <summary>
/// Make sure all fields of all skills this role can represent, and all additional actor properties
/// are part of the "flatten" structure of the serialized version
/// This is a root converter, you can only use one root converter in your serializer
/// </summary>
public class FlatProxiedRoleRootConverter : ProxiedRootConverter
{
    public override void Additionals(JsonWriter writer, IProxiedRole role, JsonSerializer rootSerializer, IReadOnlyList<JProperty> rootProperties)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global : Proxied object.
        var additionalSkills = role.Skills().Except(role.ProxiedType().GetSkills());
        var skipFields = new List<string>(); // list of items already used in one of the other skills
            skipFields.AddRange(rootProperties.Select(rp => rp.Name)); 
            
        foreach (var skill in additionalSkills)
        {
            var additonalRole = role.Actor.PresentsType(Loader.GetRoleByName(skill));
            
            var jsn = JObject.FromObject(additonalRole, //serialize the additional skill set. 
                rootSerializer); // use root serializer
            
            var additionalProps = jsn.Properties().Where(p => !skipFields.Any(skip => skip == p.Name)).ToList();
            foreach (var prop in additionalProps)
            {
                prop.WriteTo(writer);   // write each property directly
                skipFields.Add(prop.Name); // make sure this field is skipped for the next skill.
            }
        }
        
        // THEN THE ACTOR
        
        // Serialize the actor object *inline*
        if (role.Actor != null)
        {
            // Serialize actor to a temporary JObject
            var actor = JObject.FromObject(role.Actor, // serialize the actor.
                rootSerializer); // use default serializer.
            
            var referrers = role.Referrers(); // referrers configured for this role/actor combination
            foreach (var prop in actor.Properties()
                         .Where(p => 
                             !skipFields.Any(sf => sf == p.Name) &&
                             !referrers.Any(r => r.Value == p.Name) && 
                                                 !p.Name.StartsWith(Meta.__)
                         )
                    ) // skip when any of the properties is the representative of a referrer
            {
                prop.WriteTo(writer);   // write each property directly
            }
        }
        
        
    }

    public override IProxiedRole ReadJson(JsonReader reader, Type objectType, IProxiedRole existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => false;
    public override bool CanWrite => true;
    
}