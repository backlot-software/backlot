using System;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters;

/// <summary>
/// Converter for IProxiedRoles you can only have one serializer.
/// </summary>
public abstract class ProxiedRootConverter : JsonConverter<IProxiedRole>
{
    public override void WriteJson(JsonWriter writer, IProxiedRole role, JsonSerializer serializer)
    {

        if (serializer.Converters
                .Where(c => c is ProxiedRootConverter).Count() > 1)
        {
            throw new ArgumentException("To many RootConverters in the serializer.");
        }
        
        // Create a clone of the serializer without the specific rootConverter
        var rootSerializer = Strategy.CreateDeepClone(serializer); // Custom extension to copy settings (if needed)
        rootSerializer.Converters.Remove(rootSerializer.Converters.FirstOrDefault(c => c.GetType() == GetType()));
        
        // Start writing the root object
        writer.WriteStartObject();

            var jsn = JObject.FromObject(role, //serialize the role 
                rootSerializer); // use root serializer
            
            var roleProps = jsn.Properties().ToList();
            foreach (var prop in roleProps)
            {
                prop.WriteTo(writer);   // write each property directly
            }
            
            Additionals(writer, role, rootSerializer, roleProps);
        
        writer.WriteEndObject();
    }

    /// <summary>
    /// Add the additionals to the root
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="role"></param>
    /// <param name="rootSerializer">The serializer without the current converter</param>
    /// <param name="roleProperties">The properties of the skill that 'role' is currently presenting.</param>
    public abstract void Additionals(JsonWriter writer, IProxiedRole role, JsonSerializer rootSerializer, IReadOnlyList<JProperty> roleProperties);
}