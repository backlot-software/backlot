using System.Collections.Generic;
using Backlot.Core.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Http.Media.Formatters.Csv;

/// <summary>
/// Flatten nested json objects into flat dictionaries using __ to show levels.
/// </summary>
internal static class FlattenJson
{
    internal static Dictionary<string, object> Execute(JToken json, string prefix = "")
    {
        var result = new Dictionary<string, object>();

        switch (json.Type)
        {
            case JTokenType.Object:
                foreach (var property in json.Children<JProperty>())
                {
                    var key = prefix + property.Name;
                    var value = property.Value;
                    result.AddRange(Execute(value, key + Meta.__));
                }
                break;

            case JTokenType.Array:
                var index = 1;
                foreach (var item in json.Children())
                {
                    var key = prefix + index;
                    result.AddRange(Execute(item, key + Meta.__));
                    index++;
                }
                break;

            default:
                result[prefix.TrimEnd('_')] = ((JValue)json).Value!;
                break;
        }

        return result;
    }

    private static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> range)
    {
        foreach (var item in range)
        {
            dictionary[item.Key] = item.Value;
        }
    }
}