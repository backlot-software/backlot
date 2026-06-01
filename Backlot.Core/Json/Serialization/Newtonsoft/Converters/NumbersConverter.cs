#nullable enable
using System;
using Newtonsoft.Json;

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters
{
    public class NumbersConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, Convert.ToDecimal(value));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return Convert.ToDecimal(existingValue);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double);
        }
    }
}