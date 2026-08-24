namespace Backlot.Studio.Models.Api;

// One scenario endpoint described by example: the JSON you send and the JSON you get back.
// Returned by the director/scenarioschemas scenario; joined onto ScenarioItem by Scenario name.
public class ScenarioSchemaItem
{
    public string Scenario { get; set; } = null!;
    public string Endpoint { get; set; } = null!;
    public string Method { get; set; } = null!;

    // Empty for GET endpoints, which carry no body.
    public string RequestExample { get; set; } = string.Empty;
    public string ResponseExample { get; set; } = string.Empty;
}
