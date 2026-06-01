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

public class OpenApiGenerator
{
    public string GenerateOpenApiDoc(IEnumerable<IScenarioInfo> scenarios)
    {
        var openApiDoc = new JObject
        {
            ["openapi"] = "3.0.0",
            ["info"] = new JObject
            {
                ["title"] = "Dynamic API for Scenarios",
                ["version"] = $"{typeof(IDirector).Assembly.GetName().Version}",
            },
            ["paths"] = new JObject()
        };

        var paths = openApiDoc["paths"] as JObject;

        foreach (var scenario in scenarios)
        {
            var path = $"/api/role/{Loader.GetRoleName(scenario.TRole)}/{scenario.Name}";
            var isDirectorRole = typeof(IDirector).IsAssignableFrom(scenario.TRole);

            var requestType = isDirectorRole ? "get" : "post";
            var responseSchema = GenerateSchema(scenario.TResult);

            // Add request configuration
            var requestObject = new JObject
            {
                ["summary"] = $"Endpoint for scenario: {scenario.Name}",
                ["tags"] = JArray.FromObject(scenario.Tags),
                ["responses"] = new JObject
                {
                    ["200"] = new JObject
                    {
                        ["description"] = "Successful operation",
                        ["content"] = new JObject
                        {
                            ["application/json"] = new JObject
                            {
                                ["schema"] = new JObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JObject //based on JsonResponse
                                    {
                                        ["Body"] = responseSchema,
                                        ["TimeInMs"] = new JObject { ["type"] = "integer", ["format"] = "int64" },
                                        ["ExecutionTime"] = new JObject { ["type"] = "string", ["format"] = "date-time" },
                                        ["Status"] = new JObject { ["type"] = "string" }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Add requestBody for POST scenarios
            if (!isDirectorRole)
            {
                requestObject["requestBody"] = new JObject
                {
                    ["required"] = true,
                    ["content"] = new JObject
                    {
                        ["application/json"] = new JObject()
                        //{
                        //    ["schema"] = new JObject
                        //    {
                        //        ["type"] = "object",
                        //        ["properties"] = new JObject
                        //        {
                        //            ["input"] = new JObject
                        //            {
                        //                ["type"] = "object",
                        //                ["description"] = "Request body for the scenario"
                        //            }
                        //        }
                        //    }
                        //}
                    }
                };
            }

            // Attach the method and path
            if (!paths.ContainsKey(path))
            {
                paths[path] = new JObject();
            }

            (paths[path] as JObject)[requestType] = requestObject;
        }

        return JsonConvert.SerializeObject(openApiDoc, Formatting.Indented);
    }

    private HashSet<Type> _visitedTypes = [];

    private JObject GenerateSchema(Type type)
    {
        if (_visitedTypes.Contains(type)) return new JObject(); // Prevent infinite recursion
        
        _visitedTypes.Add(type);
        
        try
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                return new JObject
                {
                    ["type"] = type == typeof(string) ? "string" :
                        type == typeof(bool) ? "boolean" :
                        type == typeof(float) || type == typeof(double) || type == typeof(decimal) ? "number" :
                        "integer"
                };
            }
            else if (typeof(IEnumerable<>).IsAssignableFrom(type) || type.IsArray)
            {
                var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : type.GetElementType();
                if (elementType != null)
                    return new JObject
                    {
                        ["type"] = "array",
                        ["items"] = GenerateSchema(elementType)
                    };
            }
            else
            {
                // Complex objects
                var properties = new JObject();
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    properties[property.Name] = GenerateSchema(property.PropertyType);
                }

                return new JObject
                {
                    ["type"] = "object",
                    ["properties"] = properties
                };
            }
        }
        finally
        {
            _visitedTypes.Remove(type);
        }
        return new JObject(); // Prevent infinite recursion
    }
}