using Backlot.Studio.Core.Models.Response;

namespace Backlot.Studio.Core;

// Single point of implementation for loading the scenario catalogue used by both the Scenarios
// and Client pages. Centralising the two API reads here keeps the pages from drifting apart and
// gives them one shared degrade-on-failure policy for the best-effort scenarioschemas data.
public static class ScenarioCatalog
{
    // The registered scenarios (with their endpoints). Exceptions propagate so callers can wrap
    // the call in AuthenticatedPageModel.SafeApiCall and turn an expired credential into a login
    // redirect, exactly as the pages did inline before.
    public static async Task<IReadOnlyList<ScenarioItem>> LoadScenariosAsync(IBacklotApiClient api)
    {
        ArgumentNullException.ThrowIfNull(api);

        var envelope = await api.Play<IEnumerable<ScenarioItem>>("scenarios");
        return (envelope?.Body ?? []).ToList();
    }

    // The example request/response schemas per scenario endpoint. Best-effort: the examples are
    // reflected server-side and cost more to produce, so a failure here degrades the page to a
    // plain list instead of an error. UnauthorizedAccessException is deliberately not swallowed so
    // SafeApiCall can still redirect an expired credential to login.
    public static async Task<IReadOnlyList<ScenarioSchemaItem>> LoadSchemasAsync(
        IBacklotApiClient api, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(api);

        try
        {
            var envelope = await api.Play<IEnumerable<ScenarioSchemaItem>>("scenarioschemas");
            return (envelope?.Body ?? []).ToList();
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to load scenario schemas from Backlot API");
            return [];
        }
    }
}
