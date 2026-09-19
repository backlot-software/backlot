namespace Backlot.Defaults.Scenarios.Configuration.Models;

/// <summary>
/// INTERNAL: A result object for internal use only.
/// Can be changed without notice.
/// </summary>
/// <remarks>
/// Describes one scenario endpoint by example rather than by schema: the JSON you would send and
/// the JSON you get back. Replaces the OpenAPI document that used to be generated for the same
/// purpose -- Backlot Studio renders these directly, so no schema vocabulary is needed.
/// </remarks>
public class ScenarioSchemaResultItem
{
    /// <summary>
    /// The scenario name, matching <see cref="ScenarioResultItem.Scenario"/>.
    /// </summary>
    public string Scenario { get; init; } = null!;

    /// <summary>
    /// The endpoint this example describes; the first (most important) endpoint of the scenario.
    /// </summary>
    public string Endpoint { get; init; } = null!;

    /// <summary>
    /// The HTTP method for <see cref="Endpoint"/>. Director scenarios are played with GET,
    /// everything else with POST.
    /// </summary>
    public string Method { get; init; } = null!;

    /// <summary>
    /// An example request body as formatted JSON. Empty for GET endpoints, which carry no body.
    /// </summary>
    public string RequestExample { get; init; } = string.Empty;

    /// <summary>
    /// An example response body as formatted JSON, including the standard Backlot envelope
    /// (Body / TimeInMs / ExecutionTime / Status).
    /// </summary>
    public string ResponseExample { get; init; } = string.Empty;
}
