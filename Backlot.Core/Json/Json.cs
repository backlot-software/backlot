using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace Backlot.Core.Json
{
    /// <summary>
    /// Json serialization and converter "helpers"
    /// </summary>
    public static class Json
    {
        public static bool IsJson(this string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            // check if this string is a valid json object
            return json.TrimStart().StartsWith("{") && json.TrimEnd().EndsWith("}");
        }

        /// <summary>
        /// Serialize an object to JSON. 
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="strategy">The JSON serializer defines the serialization strategy.</param>
        /// <returns></returns>
        public static string ToJson(this object obj, JsonSerializer strategy)
        {
            if (obj == null) 
                return "null";

            if (obj is bool b)
                return b ? "true" : "false";

            if (obj.GetType().IsValueType)
                return Convert.ToString(obj, CultureInfo.InvariantCulture);

            if (obj is string)
                return $"\"{obj}\"";
            
            // default;
            
            using (var stringWriter = new StringWriter())
            {
                strategy.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }
    }
    
}
