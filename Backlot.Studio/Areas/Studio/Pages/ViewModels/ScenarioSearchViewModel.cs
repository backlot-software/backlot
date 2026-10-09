using Backlot.Studio.Core.Models.Response;

namespace Backlot.Studio.Areas.Studio.Pages.ViewModels;

// A single selectable scenario entry in the shared scenario search box: a scenario name paired
// with one endpoint of the form api/role/{role}/{scenario}.
public record ScenarioSearchOption(string Name, string Endpoint);

// Helpers for reasoning about scenario endpoint paths (api/role/{role}/{scenario}).
public static class ScenarioEndpoint
{
    // Extracts the role segment from an endpoint of the form api/role/{role}/{scenario}.
    // Returns null when the endpoint does not match that shape.
    public static string? RoleSegment(string endpoint)
    {
        var parts = endpoint.Trim('/').Split('/');
        // ["api", "role", "{role}", "{scenario}", ...]
        if (parts.Length >= 4 && parts[0] == "api" && parts[1] == "role")
            return parts[2];
        return null;
    }

    // Builds one search option per (scenario, endpoint) whose role segment is one of the given
    // skills — i.e. the scenarios a role with those skills can actually be played through.
    // Shared by the Client page and the role Detail search box.
    public static List<ScenarioSearchOption> OptionsForSkills(IEnumerable<ScenarioItem> scenarios, ISet<string> skills) =>
        scenarios
            .SelectMany(s => s.Endpoints
                .Where(e => RoleSegment(e) is string role && skills.Contains(role))
                .Select(e => new ScenarioSearchOption(s.Scenario, e)))
            .ToList();
}

// Model for the shared _ScenarioSearchBox partial. Id namespaces the element ids (…-search,
// …-list, …-combo, …-empty) so more than one box can live on a page; the matching JS is wired
// with initScenarioSearch({ id }) from site.js.
public class ScenarioSearchViewModel
{
    public string Id { get; init; } = "scenario";
    public string Placeholder { get; init; } = "Search scenarios…";
    public IReadOnlyList<ScenarioSearchOption> Options { get; init; } = [];
}
