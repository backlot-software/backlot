#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8600 
//CS8600 is done during CanConvert.

namespace Backlot.Core.Json.Serialization.Newtonsoft.Converters
{
    public class RelationConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("RelationConverter.WriteJson not implemented");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var item1 = token["Item1"]?.ToObject<RoleReference>(); // inside a converter we use native Newtonsoft.Json, no extra settings needed.
            var item2 = token["Item2"]?.ToObject<RoleReference>();
            
            return Relation.New(item1 ?? throw new InvalidOperationException("relationconverter, item 1 can not be null"), 
                item2 ??  throw new InvalidOperationException("relationconverter, item2 can not be null"));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Relation);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }
}