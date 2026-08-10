namespace Backlot.Studio.Models.Api;

public class ScenarioItem
{
    public string Scenario { get; set; } = null!;          // scenario class name
    public string Result { get; set; } = null!;            // TResult friendly name
    public string[] Roles { get; set; } = [];              // role names
    public string[] Tags { get; set; } = [];               // grouping tags (namespace-derived when not explicit)
    public string[] Endpoints { get; set; } = [];          // URL paths; director endpoint first when multi-role
    public string[] Configurations { get; set; } = [];     // named config variants
}
