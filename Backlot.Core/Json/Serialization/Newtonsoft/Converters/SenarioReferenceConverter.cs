#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8600 
//CS8600 is done during CanConvert.

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters
{
    public class SenarioReferenceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // because check is done at CanConvert
            serializer.Serialize(writer, ((ScenarioReference)value)?.Name);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Value<string>() == null || token.Value<string>() == "")
            {
                return null;
            }
            return new ScenarioReference { Name = token.Value<string>() };
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ScenarioReference);
        }
    }
}