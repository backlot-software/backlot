using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Defaults.Scenarios.Configuration.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Defaults.Scenarios.Configuration;

/// <summary>
/// Describes every scenario endpoint by example: the JSON body you send and the JSON you get back.
/// </summary>
/// <remarks>
/// This is what Backlot Studio's scenario browser renders, and what its request tester prefills a
/// body with. It deliberately produces *examples* rather than a schema document -- the reflection
/// below is the part of the retired OpenAPI generator that carried real domain knowledge, without
/// the schema vocabulary that made it expensive to maintain.
///
/// Kept out of <see cref="Scenarios"/> on purpose: three Studio pages call that scenario on every
/// load and none of them need this reflection.
/// </remarks>
[Scenario(typeof(ScenarioSchemas), access: [Access.Everyone])]
public class ScenarioSchemas : DirectorScenario<ScenarioSchemas, IEnumerable<ScenarioSchemaResultItem>>
{
    private const string ExampleUid = "000EXAMPLED64GUIDFD074EA415A6000";
    private const string ExampleText = "lorem ipsum dolor sit amet";
    private const string ExampleName = "John Doe";

    public ScenarioSchemas(IDirector role) : base(role)
    {
    }

    protected override async Task<IEnumerable<ScenarioSchemaResultItem>> ExecAsync()
    {
        // Same two sources the OpenAPI document was built from. Scenarios carries the in-process
        // ResultType and Roles carries the in-process FieldType -- both [JsonIgnore], which is why
        // this reflection has to happen here and cannot be done by the Studio itself.
        var scenarios = (await Scenarios.Play()).ToList();
        var roles = (await Roles.Play()).ToArray();

        var directorRole = typeof(IDirector).GetRoleName();
        var result = new List<ScenarioSchemaResultItem>();

        foreach (var scenario in scenarios)
        {
            var endpoint = scenario.Endpoints?.FirstOrDefault();
            if (string.IsNullOrEmpty(endpoint)) continue;

            var scenarioRoles = scenario.Roles ?? [];
            var isGet = scenarioRoles.Contains(directorRole); // director scenarios are always GET

            var cycleGuard = new HashSet<Type>();

            var requestExample = string.Empty;
            if (!isGet)
            {
                var requestRoles = scenarioRoles
                    .Select(name => roles.FirstOrDefault(r => r.Role == name))
                    .Where(r => r != null)
                    .ToArray();

                requestExample = Format(BuildRequestExample(requestRoles!, cycleGuard));
            }

            cycleGuard.Clear();
            result.Add(new ScenarioSchemaResultItem
            {
                Scenario = scenario.Scenario,
                Endpoint = endpoint,
                Method = isGet ? "GET" : "POST",
                RequestExample = requestExample,
                ResponseExample = Format(BuildResponseExample(scenario.ResultType, cycleGuard))
            });
        }

        return result;
    }

    private static string Format(JToken token) => token.ToString(Formatting.Indented);

    /// <summary>
    /// A single role posts its fields flat; multiple roles are each nested under their role name,
    /// mirroring how <c>GetRoles.ForPostRequest</c> reads a request body.
    /// </summary>
    private static JToken BuildRequestExample(RoleResultItem[] requestRoles, HashSet<Type> cycleGuard)
    {
        if (requestRoles.Length == 0) return new JObject();

        if (requestRoles.Length == 1) return BuildRoleExample(requestRoles[0], cycleGuard);

        var body = new JObject();
        foreach (var role in requestRoles)
            body[role.Role] = BuildRoleExample(role, cycleGuard);

        return body;
    }

    /// <summary>
    /// The standard Backlot response envelope wrapped around an example of the scenario's result.
    /// </summary>
    private static JToken BuildResponseExample(Type resultType, HashSet<Type> cycleGuard) =>
        new JObject
        {
            ["Body"] = BuildValueExample("Body", resultType, cycleGuard),
            ["TimeInMs"] = 12,
            ["ExecutionTime"] = DateTimeOffset.Now.ToString("O"),
            ["Status"] = "OK"
        };

    private static JToken BuildRoleExample(RoleResultItem requestRole, HashSet<Type> cycleGuard)
    {
        if (!Loader.TryGetRoleByName(requestRole.Role, out var roleType)) return new JObject();

        return BuildObjectExample(roleType, cycleGuard, () =>
        {
            var body = new JObject();

            // Calculated fields are never persisted, so posting them is meaningless.
            foreach (var field in requestRole.Fields.Where(f => !IsCalculated(f)))
                body[field.Field] = BuildValueExample(field.Field, field.FieldType, cycleGuard);

            return body;
        });
    }

    private static bool IsCalculated(FieldResultItem field) =>
        field.Characteristics.Any(c =>
            c.Characteristic != null &&
            c.Characteristic.Equals(
                nameof(CalculatedAttribute)[..^"attribute".Length],
                StringComparison.InvariantCultureIgnoreCase));

    private static JToken BuildValueExample(string propertyName, Type type, HashSet<Type> cycleGuard)
    {
        var known = KnownExample(propertyName, type);
        if (known != null) return known;

        var elementType = EnumerableElementType(type);
        if (elementType != null)
            return new JArray(BuildComplexExample(elementType, cycleGuard));

        return BuildComplexExample(type, cycleGuard);
    }

    private static JToken BuildComplexExample(Type type, HashSet<Type> cycleGuard) =>
        BuildObjectExample(type, cycleGuard, () =>
        {
            var body = new JObject();
            var seen = new HashSet<string>();

            foreach (var property in AllProperties(type))
            {
                if (!seen.Add(property.Name)) continue;
                body[property.Name] = BuildValueExample(property.Name, property.PropertyType, cycleGuard);
            }

            if (type.GetInterfaces().Any(i => typeof(IRole).IsAssignableFrom(i)))
                body[Meta.__Skills] = new JArray("Role", "Permission", "...");

            return body;
        });

    /// <summary>
    /// Guards against self-referencing role graphs. The type is removed again on the way out so a
    /// type appearing twice as a sibling is still expanded the second time -- only true recursion
    /// is cut off.
    /// </summary>
    private static JToken BuildObjectExample(Type guardType, HashSet<Type> cycleGuard, Func<JObject> build)
    {
        if (guardType == null || !cycleGuard.Add(guardType)) return new JObject();

        try
        {
            return build();
        }
        finally
        {
            cycleGuard.Remove(guardType);
        }
    }

    private static Type? EnumerableElementType(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>) ||
                definition == typeof(ICollection<>) ||
                definition == typeof(IList<>) ||
                definition == typeof(List<>))
                return type.GetGenericArguments()[0];
        }

        return type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    /// <summary>
    /// All public instance properties, including those inherited through the interface hierarchy --
    /// necessary because GetProperties() on an interface does NOT walk its base interfaces.
    /// Properties ignored by either JSON serializer are left out, since they never reach the wire.
    /// </summary>
    private static IEnumerable<PropertyInfo> AllProperties(Type type)
    {
        if (type == null) return [];

        if (!type.IsInterface)
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => !IsJsonIgnored(p));

        return new[] { type }
            .Concat(type.GetInterfaces())
            .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => !IsJsonIgnored(p));

        static bool IsJsonIgnored(PropertyInfo p) =>
            p.IsDefined(typeof(JsonIgnoreAttribute), inherit: true) ||
            p.IsDefined(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), inherit: true);
    }

    /// <summary>
    /// Example values for primitives and the Backlot meta fields. Recognisable placeholders beat
    /// type names here: the operator copies these straight into the request tester.
    /// </summary>
    private static JToken? KnownExample(string propertyName, Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (propertyName == Meta.__Permission)
            return new JObject
            {
                ["CanCreate"] = true,
                ["CanRead"] = true,
                ["CanWrite"] = true,
                ["Encrypted"] = "****"
            };

        if (t == typeof(string))
            return propertyName switch
            {
                nameof(IPersist.Uid) => ExampleUid,
                nameof(IPersist.Name) => ExampleName,
                _ => ExampleText
            };

        if (t == typeof(int) || t == typeof(long) || t == typeof(short) ||
            t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) ||
            t == typeof(byte) || t == typeof(sbyte))
            return 123;

        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return 12.34;

        if (t == typeof(bool)) return true;
        if (t == typeof(Guid)) return Guid.Empty.ToString();
        if (t == typeof(RoleReference)) return ExampleUid;
        if (t == typeof(DateTimeOffset) || t == typeof(DateTime)) return DateTimeOffset.Now.ToString("O");
        if (t == typeof(TimeSpan)) return TimeSpan.Zero.ToString();
        if (t.IsEnum) return Enum.GetNames(t).FirstOrDefault() ?? string.Empty;
        if (t == typeof(JObject)) return new JObject();
        if (t == typeof(object)) return new JObject();

        return null;
    }
}
