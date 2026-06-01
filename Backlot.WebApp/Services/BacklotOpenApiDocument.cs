using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;
using Backlot.Defaults.Scenarios.Configuration;
using Backlot.Defaults.Scenarios.Configuration.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Experimental.WebApp.Services;

public class BacklotOpenApiDocument : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var scenarios = (await Scenarios.Play()).ToList();
            var roles = (await Roles.Play()).ToArray();

            var definitions = BuildDefinitions(scenarios, roles);
            var cycleGuard = new HashSet<Type>();

            // info
            document.Info = new OpenApiInfo
            {
                Title = "Backlot API Documentation",
                Description =
                    "This is the OpenAPI documentation for all the endpoints exposed by the Backlot API. This documentation is currently in experimental phase and is not meant to be used in production.",
                Version = $"{typeof(IDirector).Assembly.GetName().Version}"
            };

            // components / security schemes
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["ApiKeyAuth"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "Authorization"
                }
            };

            // security
            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKeyAuth", document)] = new List<string>()
                }
            ];

            // paths
            document.Paths = new OpenApiPaths();

            foreach (var scenario in scenarios)
            {
                var path = scenario.Endpoints?.FirstOrDefault();
                if (string.IsNullOrEmpty(path) || !definitions.TryGetValue(path, out var def)) continue;

                var scenarioRoles = scenario.Roles ?? Array.Empty<string>();
                var isGetRequest =
                    scenarioRoles.Contains(typeof(IDirector).GetRoleName()); // Director is always a GET endpoint

                var operation = new OpenApiOperation
                {
                    Summary = BuildSummary(scenario, scenarioRoles),
                    Tags = new HashSet<OpenApiTagReference>()
                };

                // tags
                foreach (var tag in scenario.Tags ?? [])
                    operation.Tags.Add(new OpenApiTagReference(tag, document));

                // request body (skipped for Director/GET endpoints)
                if (!isGetRequest)
                {
                    cycleGuard.Clear();
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = BuildRequestSchema(def.RequestTypes, cycleGuard)
                            }
                        }
                    };
                }

                // responses
                cycleGuard.Clear();
                var responseProperties = new Dictionary<string, IOpenApiSchema>();
                BuildPropertySchema(responseProperties, "Body", def.ResponseType, cycleGuard);

                responseProperties["TimeInMs"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int64"
                };

                responseProperties["ExecutionTime"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "date-time"
                };

                responseProperties["Status"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                };

                operation.Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Successful operation",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.Object,
                                    Properties = responseProperties
                                }
                            }
                        }
                    }
                };

                document.Paths[path] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [isGetRequest ? HttpMethod.Get : HttpMethod.Post] = operation
                    }
                };
            }
        }
        catch (Exception ex)
        {
            document.Info = new OpenApiInfo
            {
                Title = "API Error",
                Version = "1.0.0",
                Description = ex.ToString()
            };
            document.Paths = new OpenApiPaths();
            document.Components = null;
            document.Security = null;
        }
    }

    private static string BuildSummary(ScenarioResultItem scenario, string[] scenarioRoles)
    {
        if (scenarioRoles.Length == 1)
        {
            return scenarioRoles[0] == typeof(IDirector).GetRoleName()
                ? $"{scenario.Scenario}.{nameof(DirectorScenario<,>.Play)}()"
                : $"{scenario.Scenario}.{nameof(Scenario<,,>.Play)}({scenarioRoles[0]})";
        }

        return
            $"{scenario.Scenario}{string.Join("", scenarioRoles.Select(r => $".With({r})"))}.Play())";
    }

    /// <summary>
    /// Builds the working definitions dictionary used during serialization.
    /// Auto-derives an <see cref="EndpointDefinition"/> for each scenario path, then
    /// overlays any manually registered entries from <see cref="EndpointDefinitions"/>.
    /// </summary>
    public static Dictionary<string, EndpointDefinition> BuildDefinitions(
        IEnumerable<ScenarioResultItem> scenarios, RoleResultItem[] availableRoles)
    {
        var definitions = scenarios
            .GroupBy(s => s.Endpoints?.FirstOrDefault())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => BuildDefaultDefinition(g.First(), availableRoles));

        return definitions;
    }

    /// <summary>
    /// Auto-derives an <see cref="EndpointDefinition"/> from a scenario by resolving
    /// its role/result name strings to C# types via <see cref="Loader"/>.
    /// </summary>
    private static EndpointDefinition BuildDefaultDefinition(ScenarioResultItem scenario,
        RoleResultItem[] availableRoles)
    {
        var roles = (scenario.Roles ?? Array.Empty<string>())
            .Select(r => availableRoles.FirstOrDefault(ri => ri.Role == r))
            .Where(t => t != null)
            .ToArray(); // request types are always roles.

        return new EndpointDefinition
        {
            RequestTypes = roles,
            ResponseType = scenario.ResultType
        };
    }

    /// <summary>
    /// Builds the request body schema from typed definitions.
    /// Single type: flat properties. Multiple types: each nested under its role name.
    /// </summary>
    private static OpenApiSchema BuildRequestSchema(RoleResultItem[] requestTypes, HashSet<Type> cycleGuard)
    {
        if (requestTypes is not { Length: > 0 })
        {
            return new OpenApiSchema { Type = JsonSchemaType.Object };
        }

        if (requestTypes.Length == 1)
        {
            return BuildRequestSchemaForRole(requestTypes[0], cycleGuard);
        }

        // Multiple roles: nest each under its role name
        var properties = new Dictionary<string, IOpenApiSchema>();
        foreach (var t in requestTypes)
            properties[t.Role] = BuildRequestSchemaForRole(t, cycleGuard);

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = properties
        };
    }

    /// <summary>
    /// Request schema based on a single role.
    /// </summary>
    private static OpenApiSchema BuildRequestSchemaForRole(RoleResultItem requestRole, HashSet<Type> cycleGuard)
    {
        if (requestRole == null) return new OpenApiSchema { Type = JsonSchemaType.Object };

        var type = Loader.TryGetRoleByName(requestRole.Role, out var role) ? role : null;
        if (type == null) return new OpenApiSchema { Type = JsonSchemaType.Object };

        return BuildObjectSchema(type, cycleGuard, () =>
        {
            var props = new Dictionary<string, IOpenApiSchema>();
            foreach (var field in requestRole.Fields.Where(
                         f => !f.Characteristics.Any(
                             c => c.Characteristic != null
                                  && c.Characteristic.Equals(
                                      nameof(CalculatedAttribute)[..^"attribute".Length],
                                      StringComparison.InvariantCultureIgnoreCase))))
            {
                BuildPropertySchema(props, field.Field, field.FieldType, cycleGuard);
            }

            return props;
        });
    }

    /// <summary>
    /// Unified schema builder. Callers supply the guard type and a callback that
    /// returns the properties dictionary.
    /// </summary>
    private static OpenApiSchema BuildObjectSchema(Type guardType, HashSet<Type> cycleGuard,
        Func<Dictionary<string, IOpenApiSchema>> buildProperties)
    {
        if (guardType == null || !cycleGuard.Add(guardType))
        {
            return new OpenApiSchema { Type = JsonSchemaType.Object };
        }

        try
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = buildProperties()
            };
        }
        finally
        {
            cycleGuard.Remove(guardType);
        }
    }

    /// <summary>
    /// Builds the schema for a property and adds it to the properties dictionary.
    /// </summary>
    private static void BuildPropertySchema(Dictionary<string, IOpenApiSchema> properties, string propertyName,
        Type type, HashSet<Type> cycleGuard)
    {
        // known templates
        var known = CreateKnownSchema(propertyName, type);
        if (known != null)
        {
            properties[propertyName] = known;
            return;
        }

        // enumerable
        var array = CreateArraySchema(type, cycleGuard);
        if (array != null)
        {
            properties[propertyName] = array;
            return;
        }

        // complex object
        properties[propertyName] = BuildObjectSchema(type, cycleGuard, () =>
        {
            var props = new Dictionary<string, IOpenApiSchema>();
            var seen = new HashSet<string>();
            foreach (var property in GetAllProperties(type))
            {
                if (!seen.Add(property.Name)) continue; // skip duplicates
                BuildPropertySchema(props, property.Name, property.PropertyType, cycleGuard);
            }

            if (type.GetInterfaces().Any(i => typeof(IRole).IsAssignableFrom(i)))
            {
                props[Meta.__Skills] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Example = new JsonArray("Role", "Permission", "...")
                };
            }

            return props;
        });
    }

    private static OpenApiSchema CreateArraySchema(Type type, HashSet<Type> cycleGuard)
    {
        var elementType = GetEnumerableElementType(type);
        if (elementType == null) return null;

        var itemSchema = BuildObjectSchema(elementType, cycleGuard, () =>
        {
            var props = new Dictionary<string, IOpenApiSchema>();
            var seen = new HashSet<string>();
            foreach (var property in GetAllProperties(elementType))
            {
                if (!seen.Add(property.Name)) continue;
                BuildPropertySchema(props, property.Name, property.PropertyType, cycleGuard);
            }

            return props;
        });

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = itemSchema
        };
    }

    private static Type GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) ||
                def == typeof(ICollection<>) ||
                def == typeof(IList<>) ||
                def == typeof(List<>))
                return type.GetGenericArguments()[0];
        }

        // check implemented interfaces
        return type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    // TODO: Exclude all ignored properties!! currently when Type is a role ignored properties are still part of the example results.

    /// <summary>
    /// Returns all public instance properties, including those from base classes and
    /// all inherited interfaces -- necessary because GetProperties() on an interface
    /// does NOT walk the interface hierarchy.
    /// </summary>
    private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
    {
        if (type == null) return [];

        if (!type.IsInterface)
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !IsJsonIgnored(p));

        // For interfaces: walk the full hierarchy manually
        return new[] { type }
            .Concat(type.GetInterfaces())
            .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => !IsJsonIgnored(p));

        static bool IsJsonIgnored(PropertyInfo p)
        {
            return p.IsDefined(typeof(JsonIgnoreAttribute), inherit: true) ||
                   p.IsDefined(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), inherit: true);
        }
    }

    /// <summary>
    /// Creates schemas for primitives and other known types.
    /// </summary>
    private static OpenApiSchema CreateKnownSchema(string propName, Type type)
    {
        // unwrap Nullable<T> -> T
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (propName == Meta.__Permission)
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Example = new JsonObject
                {
                    ["CanCreate"] = true,
                    ["CanRead"] = true,
                    ["CanWrite"] = true,
                    ["Encrypted"] = "****"
                }
            };
        }

        if (t == typeof(string))
        {
            var txtVal = "lorem ipsum dolor sit amet";

            if (propName == nameof(IPersist.Uid))
            {
                txtVal = "000EXAMPLED64GUIDFD074EA415A6000";
            }
            else if (propName == nameof(IPersist.Name))
            {
                txtVal = "John Doe";
            }

            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Example = (JsonNode)txtVal
            };
        }

        if (t == typeof(int) || t == typeof(long) || t == typeof(short) ||
            t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) ||
            t == typeof(byte) || t == typeof(sbyte))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Example = (JsonNode)123
            };
        }

        if (t == typeof(bool))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Boolean,
                Example = (JsonNode)true
            };
        }

        if (t == typeof(RoleReference))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Example = (JsonNode)"000EXAMPLED64GUIDFD074EA415A6000"
            };
        }

        if (t == typeof(DateTimeOffset))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "date-time"
            };
        }

        if (t == typeof(JObject))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Object
            };
        }

        return null;
    }

    public void Dispose()
    {
    }
}
