using Newtonsoft.Json;

namespace Backlot.Defaults.Scenarios.Configuration.Models;

/// <summary>
/// INTERNAL: A result object for internal use only.
/// Can be changed without notice.
/// </summary>
public class ScenarioResultItem
{
    /// <summary>
    /// The scenario name
    /// </summary>
    public string Scenario { get; init; } = null!;
    
    /// <summary>
    /// The result type, can be any object.
    /// </summary>
    public string Result { get; init; } = null!;
    
    [JsonIgnore]
    public Type ResultType { get; init; } = null!;
    
    /// <summary>
    /// The roles used to execute the scenario
    /// The request objects.
    /// </summary>
    public string[] Roles { get; init; } = null!;
    
    /// <summary>
    /// Tags for categorizing the scenario when creating overviews or such.
    /// </summary>
    public string[] Tags { get; init; } = null!;
    
    /// <summary>
    /// The endpoints you can access the scenario with, when using Backlot.Http
    /// The most important endpoint is always first all others are synonyms for the same scenario.
    /// </summary>
    public string[] Endpoints { get; init; } = null!;
    
    /// <summary>
    /// The different configuration parameters possible.
    /// </summary>
    public string[] Configurations { get; init; }  = null!;
}