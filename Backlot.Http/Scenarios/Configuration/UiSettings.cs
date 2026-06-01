using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Newtonsoft.Json.Linq;

namespace Backlot.Http.Scenarios.Configuration;

/// <summary>
/// Returns the json representation of the Uisettings for this server instance.
/// You can create specific ui settings per user group, which are merged with the default.
/// </summary>
[Scenario(typeof(UiSettings), access: [Access.Everyone])]
public class UiSettings(
    IDirector role,
    IFileSystem fileSystem) : DirectorScenario<UiSettings, JObject>(role)
{
    protected override async Task<JObject> ExecAsync()
    {
        // todo: return different settings based on user role
        
        var content = await fileSystem.GetFileContentAsync("uisettings.json");
        // create JObject from content
        var json = JObject.Parse(content);
        
        var backlotversion = typeof(IDirector).Assembly.GetName().Version;
        json.Add("Version", backlotversion?.ToString());
        
        // return object
        return json;
    }
}