using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Services;
using Newtonsoft.Json.Linq;

namespace Backlot.Http;

/// <summary>
/// JsonSettingsManager, to manage configurations inside a json file
/// - Depends on Autofac. Autofac configuration is managed inside the "Container" object.
/// - Depends on Newtonsoft Json to manage the file based Json file.
/// - File pattern; {Environment}.jsonsettings.json
/// </summary>
public class JsonSettingsManager : BaseSettingsManager
{
    private const string ContainerName = "Container";

    private string SourceFileName => $"{Environment}.jsonsettings.json";

    private readonly IFileSystem _fileSystem;

    private JToken? _content;

    private JToken Content => _content ??= GetContentFromFile();

    private JToken Container => Content[ContainerName] ?? throw new ArgumentException($"Container with name \"{ContainerName}\" doesn't exist");

    private JToken GetContentFromFile()
    {
        return JToken.Parse(_fileSystem.GetFileContent(SourceFileName));
    }

    public override IEnumerable<ConfigurationInfo> GetAllSettings()
    {
        var dic = new Dictionary<string, Type>();

        Parallel.ForEach(Loader.AllTypes, type =>
        {
            var ctor = type.GetConstructors() //constructors
                .Where(pinfo => pinfo.GetCustomAttributes(false)
                    .OfType<ConfigurableAttribute>().Any()).MaxBy(c => c.GetParameters().Length); //always select the constructor with the most parameters.

            if (ctor != null)
            {

                foreach (var par in ctor.GetParameters()) //all configurable constructor parameters
                {
                    if (!typeof(IRole).IsAssignableFrom(par.ParameterType)) //skip role parameters
                    {
                        dic.TryAdd($"{type.FullName}.{par.Name}", par.ParameterType);
                    }
                }
            }

            foreach (var prop in type //properties
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(pinfo => pinfo.GetCustomAttributes(false)
                             .OfType<ConfigurableAttribute>().Any()))
            {
                dic.TryAdd($"{type.FullName}.{prop.Name}", prop.PropertyType);
            }
        });

        foreach (var parameter in ContainerParameters()) //all (autofac) configured constructor parameters.
        {
            dic.TryAdd(parameter.Key, parameter.Value);
        }

        // add missing custom configurations

        foreach (var field in new JsonFieldsCollector(Content).GetAllFields())
        {
            if (!field.Key.StartsWith(ContainerName))
            {
                dic.TryAdd(field.Key, typeof(string));
            }
        }

        var ret = new List<ConfigurationInfo>();
        ret.AddRange(
            dic.Select(pair => new ConfigurationInfo
            {
                Name = pair.Key,
                ConfigurationType = pair.Value,
                Value = Get(pair.Key),
                ReadOnly = false
            }));

        return ret;
    }

    /// <summary>
    /// Get settings stored inside the "Container" configurations parameter lists.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<KeyValuePair<string, Type>> ContainerParameters()
    {
        if (Container["components"] is JArray components) //todo: see double code with; TryGetFromContainer
        {
            foreach (var compontent in components)
            {
                var componentType = compontent["type"];
                var componentTypeStr = componentType?.Value<string>();
                if (compontent["parameters"] is JContainer parameters)
                {
                    foreach (var parameter in parameters.OfType<JProperty>())
                    {
                        yield return new KeyValuePair<string, Type>
                        ($"{componentTypeStr?.Substring(0, componentTypeStr.IndexOf(','))}.{parameter.Name}",
                            typeof(string));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the named configurations of the given path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public override string[] GetNames(string path)
    {
        var parts = path.Split(".");

        var current = Content;

        foreach (var part in parts) //search in default json
        {
            current = current?[part];

            if (current != null && !current.Children().Any())
                break;
        }

        if (current != null)
            return current.Children().Where(j => j.First() is not JValue).OfType<JProperty>().Select(n => n.Name)
                .ToArray();

        return Enumerable.Empty<string>().ToArray();
    }

    public override Stream GetContainerStream()
    {
        var json = Container.ToString();

        return new MemoryStream(System.Text.Encoding.Default.GetBytes(json));
    }

    public override async Task Update(IConfigurationInfo configuration)
    {
        var path = configuration.Name;

        //todo: if (GetAllSettings().FirstOrDefault(s => s.Name.Equals(path)) == null || when path.split -1 is not a setting)
        // either the configuration it self or when named (the last parent) need to be a valid configuration..
        //return; //stop updating when this is not a setting

        //check for existing stored item; 
        var parts = path.Split(".");
        if (Content.SelectToken(string.Join(".", parts.Take(parts.Length - 1))) is JObject jobject)
        {
            var value = configuration.Value.ToString() ?? string.Empty;
            jobject[parts.Last()] = value;
            await _fileSystem.UpdateFileAsync(SourceFileName, Content.ToString());
            //update data from file 
            _content = GetContentFromFile();
        }


        //in all other cases create the item.
        UpdateJson(configuration.Name, configuration.Value.ToString() ?? string.Empty);
    }

    private void UpdateJson(string toAdd, string valueToAdd)
    {
        var pathParts = toAdd.Split('.');
        var node = Content;
        for (var i = 0; i < pathParts.Length; i++)
        {
            var pathPart = pathParts[i];
            var partNode = node!.SelectToken(pathPart);
            if (partNode == null && i < pathParts.Length - 1)
            {
                ((JObject)node).Add(pathPart, new JObject());
                partNode = node.SelectToken(pathPart);
            }
            else if (partNode == null && i == pathParts.Length - 1)
            {
                ((JObject)node).Add(pathPart, valueToAdd);
                partNode = node.SelectToken(pathPart);
            }
            node = partNode;
        }
    }

    public override Task Delete(IConfigurationInfo configuration)
    {
        //todo: implement
        return Task.CompletedTask;
    }

    public override string? Get(string path)
    {
        var parts = path.Split(".");

        var current = Content;
        foreach (var part in parts) //search in default json
        {
            var jt = current?[part] ?? 
                     current?.Parent?.Parent?[part]; //  get settings from parent/default when not found in named configuration.

            if(jt is JValue jv) // when jt is a value return that string.
                return jv.Value<string>();

            if(jt != null && jt.Children().Any())
                current = jt;
        }

        return TryGetFromContainer(path, out var val) ? val : string.Empty;
    }

    private bool TryGetFromContainer(string name, out string? value)
    {
        if (Container["components"] is JArray components)
        {
            foreach (var compontent in components)
            {
                var componentType = compontent["type"];
                var componentTypeStr = componentType?.Value<string>();

                if (componentTypeStr == null) continue;

                var type = Type.GetType(componentTypeStr);

                if (type == null || type.FullName == null) continue;

                if (name.StartsWith(type.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (compontent["parameters"] is JContainer parameters)
                    {
                        foreach (var parameter in parameters.OfType<JProperty>())
                        {
                            if (name.EndsWith(parameter.Name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                value = parameter.Value.Value<string>();
                                return true;
                            }
                        }
                    }
                }
            }
        }

        value = null;
        return false;
    }

    public JsonSettingsManager(string env, IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        Environment = env;
    }
}

internal class JsonFieldsCollector
{
    private readonly Dictionary<string, JValue> _fields;

    public JsonFieldsCollector(JToken token)
    {
        _fields = new Dictionary<string, JValue>();
        CollectFields(token);
    }

    private void CollectFields(JToken jToken)
    {
        switch (jToken.Type)
        {
            case JTokenType.Object:
                foreach (var child in jToken.Children<JProperty>())
                    CollectFields(child);
                break;
            case JTokenType.Array:
                foreach (var child in jToken.Children())
                    CollectFields(child);
                break;
            case JTokenType.Property:
                CollectFields(((JProperty)jToken).Value);
                break;
            default:
                _fields.Add(jToken.Path, (JValue)jToken);
                break;
        }
    }

    public IEnumerable<KeyValuePair<string, JValue>> GetAllFields() => _fields;
}