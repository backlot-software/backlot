#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8600

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters
{
    public class RoleReferenceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // code when we like to turn this into a ReadWrite Converter:
            
            //var reference = (RoleReference) value!;
            //
            //if (string.IsNullOrWhiteSpace(reference.Info))
            //{
            //    
            //    var rep = ServiceLocator.Get<IPersistedRoleRepository>();
            //
            //    if (rep.TryGet<IPersist>(reference.Uid, out var role))
            //    {
            //        value = role.GetReference();
            //    }
            //}
            //
            //var serializedJson = JsonConvert.SerializeObject(value);
            //writer.WriteRawValue(serializedJson);

            throw new NotImplementedException("This is a ReadOnly converter.");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            
            if (token.HasValues) // the token already is a serialized RoleReference
            {
                // default: 
                // Deserialize the JSON data using Newtonsoft.Json
                return JsonConvert.DeserializeObject(token.ToString(), objectType);
            }

            if (!(token is JValue)) return null;
            
            if (token.Value<string>() == null || token.Value<string>() == "")
            {
                return null;
            }
            
            // in case the token is just a string, we assume it is a uid and create a new RoleReference.

            var uid = token.Value<string>();
            return new RoleReference() { Uid = uid, Info = $"Reference to '{uid}'" }; // for performance reasons we are not loading info here, because in code execution it is not needed.
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RoleReference);
        }
    }
}