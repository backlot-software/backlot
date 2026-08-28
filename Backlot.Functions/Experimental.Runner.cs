using System.Net;
using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Experimental.Functions;

public class Runner
{
    [Function("openapi-doc")]
    public HttpResponseData GetOpenApi([HttpTrigger(AuthorizationLevel.Function, "get", Route = "openapi.json")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=UTF-8");

        var scenarios = Loader.GetAllScenarios();

        var generator = new OpenApiGenerator();
        var openApiJson = generator.GenerateOpenApiDoc(scenarios);


        response.WriteString(openApiJson);

        return response;
    }
}

// remove: public class OpenApiGenerator
// remove: {
// remove:     public string GenerateOpenApiDoc(IEnumerable<IScenarioInfo> scenarios)
// remove:     {
// remove:         var openApiDoc = new JObject
// remove:         {
// remove:             ["openapi"] = "3.0.0",
// remove:             ["info"] = new JObject
// remove:             {
// remove:                 ["title"] = "Dynamic API for Scenarios",
// remove:                 ["version"] = $"{typeof(IDirector).Assembly.GetName().Version}",
// remove:             },
// remove:             ["paths"] = new JObject()
// remove:         };
// remove: 
// remove:         var paths = openApiDoc["paths"] as JObject;
// remove: 
// remove:         foreach (var scenario in scenarios)
// remove:         {
// remove:             var path = $"/api/role/{Loader.GetRoleName(scenario.TRole)}/{scenario.Name}";
// remove:             var isDirectorRole = typeof(IDirector).IsAssignableFrom(scenario.TRole);
// remove: 
// remove:             var requestType = isDirectorRole ? "get" : "post";
// remove:             var responseSchema = GenerateSchema(scenario.TResult);
// remove: 
// remove:             // Add request configuration
// remove:             var requestObject = new JObject
// remove:             {
// remove:                 ["summary"] = $"Endpoint for scenario: {scenario.Name}",
// remove:                 ["tags"] = JArray.FromObject(scenario.Tags),
// remove:                 ["responses"] = new JObject
// remove:                 {
// remove:                     ["200"] = new JObject
// remove:                     {
// remove:                         ["description"] = "Successful operation",
// remove:                         ["content"] = new JObject
// remove:                         {
// remove:                             ["application/json"] = new JObject
// remove:                             {
// remove:                                 ["schema"] = new JObject
// remove:                                 {
// remove:                                     ["type"] = "object",
// remove:                                     ["properties"] = new JObject //based on JsonResponse
// remove:                                     {
// remove:                                         ["Body"] = responseSchema,
// remove:                                         ["TimeInMs"] = new JObject { ["type"] = "integer", ["format"] = "int64" },
// remove:                                         ["ExecutionTime"] = new JObject { ["type"] = "string", ["format"] = "date-time" },
// remove:                                         ["Status"] = new JObject { ["type"] = "string" }
// remove:                                     }
// remove:                                 }
// remove:                             }
// remove:                         }
// remove:                     }
// remove:                 }
// remove:             };
// remove: 
// remove:             // Add requestBody for POST scenarios
// remove:             if (!isDirectorRole)
// remove:             {
// remove:                 requestObject["requestBody"] = new JObject
// remove:                 {
// remove:                     ["required"] = true,
// remove:                     ["content"] = new JObject
// remove:                     {
// remove:                         ["application/json"] = new JObject()
// remove:                         //{
// remove:                         //    ["schema"] = new JObject
// remove:                         //    {
// remove:                         //        ["type"] = "object",
// remove:                         //        ["properties"] = new JObject
// remove:                         //        {
// remove:                         //            ["input"] = new JObject
// remove:                         //            {
// remove:                         //                ["type"] = "object",
// remove:                         //                ["description"] = "Request body for the scenario"
// remove:                         //            }
// remove:                         //        }
// remove:                         //    }
// remove:                         //}
// remove:                     }
// remove:                 };
// remove:             }
// remove: 
// remove:             // Attach the method and path
// remove:             if (!paths.ContainsKey(path))
// remove:             {
// remove:                 paths[path] = new JObject();
// remove:             }
// remove: 
// remove:             (paths[path] as JObject)[requestType] = requestObject;
// remove:         }
// remove: 
// remove:         return JsonConvert.SerializeObject(openApiDoc, Formatting.Indented);
// remove:     }
// remove: 
// remove:     private HashSet<Type> _visitedTypes = [];
// remove: 
// remove:     private JObject GenerateSchema(Type type)
// remove:     {
// remove:         if (_visitedTypes.Contains(type)) return new JObject(); // Prevent infinite recursion
// remove:         
// remove:         _visitedTypes.Add(type);
// remove:         
// remove:         try
// remove:         {
// remove:             if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
// remove:             {
// remove:                 return new JObject
// remove:                 {
// remove:                     ["type"] = type == typeof(string) ? "string" :
// remove:                         type == typeof(bool) ? "boolean" :
// remove:                         type == typeof(float) || type == typeof(double) || type == typeof(decimal) ? "number" :
// remove:                         "integer"
// remove:                 };
// remove:             }
// remove:             else if (typeof(IEnumerable<>).IsAssignableFrom(type) || type.IsArray)
// remove:             {
// remove:                 var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : type.GetElementType();
// remove:                 if (elementType != null)
// remove:                     return new JObject
// remove:                     {
// remove:                         ["type"] = "array",
// remove:                         ["items"] = GenerateSchema(elementType)
// remove:                     };
// remove:             }
// remove:             else
// remove:             {
// remove:                 // Complex objects
// remove:                 var properties = new JObject();
// remove:                 foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
// remove:                 {
// remove:                     properties[property.Name] = GenerateSchema(property.PropertyType);
// remove:                 }
// remove: 
// remove:                 return new JObject
// remove:                 {
// remove:                     ["type"] = "object",
// remove:                     ["properties"] = properties
// remove:                 };
// remove:             }
// remove:         }
// remove:         finally
// remove:         {
// remove:             _visitedTypes.Remove(type);
// remove:         }
// remove:         return new JObject(); // Prevent infinite recursion
// remove:     }
// remove: }