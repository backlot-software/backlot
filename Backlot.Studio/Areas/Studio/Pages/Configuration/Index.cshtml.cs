using System.Text.Json;
using Backlot.Studio.Areas.Studio.Pages.ViewModels;
using Backlot.Studio.Core;
using Backlot.Studio.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Areas.Studio.Pages.Configuration;

// Configuration — browse and manage the Backlot API's configuration entries. The flat list from
// GET /api/role/director/configurationinfos is turned into a namespace tree (left nav) plus a
// per-class panel of editable properties. Each property has a required default value ({class}.
// {property}) and optional named alternatives ({class}.{name}.{property}); both are persisted with
// POST /api/role/configurationinfo/tryupdateconfiguration. Only String and Boolean types are edited.
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public string ApiBaseUrl { get; }

    public ConfigTreeNode Root { get; } = new();
    // Class nodes (those owning properties), flattened for rendering one editor panel each.
    public List<ConfigTreeNode> Classes { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
        ApiBaseUrl = api.BaseUrl.AbsoluteUri;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();
        try
        {
            var (result, redirect) = await SafeApiCall(async () =>
                await _api.Play<List<ConfigurationInfo>>("configurationinfos"));
            if (redirect != null) return redirect;

            BuildTree(result?.Body ?? []);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load configuration from Backlot API");
            ErrorMessage = "Could not load configuration. Check that the Backlot API is reachable and that your credentials are valid.";
        }

        return Page();
    }

    // Turns the flat entry list into properties + named alternatives, then a namespace tree.
    private void BuildTree(IEnumerable<ConfigurationInfo> infos)
    {
        var all = infos.ToList();
        var allNames = new HashSet<string>(all.Select(i => i.Name), StringComparer.Ordinal);

        // Classify: an entry is a named alternative when dropping its second-to-last segment yields
        // another entry that exists in the list (the default it belongs to: {class}.{name}.{property}
        // → {class}.{property}). Everything else is a default property.
        var properties = new Dictionary<string, ConfigProperty>(StringComparer.Ordinal);
        var namedPending = new List<(string BaseName, NamedConfig Named)>();

        foreach (var info in all)
        {
            var segments = info.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2) continue; // not a {class}.{property} shape — skip defensively

            var baseName = TryGetBaseName(segments);
            if (baseName != null && allNames.Contains(baseName))
            {
                namedPending.Add((baseName, new NamedConfig
                {
                    Name = info.Name,
                    ConfigName = segments[^2],
                    Value = info.Value
                }));
                continue;
            }

            properties[info.Name] = new ConfigProperty
            {
                Name = info.Name,
                PropertyName = segments[^1],
                ClassPath = string.Join('.', segments[..^1]),
                Value = info.Value,
                IsBoolean = info.IsBoolean,
                ReadOnly = info.ReadOnly
            };
        }

        // Attach named alternatives to their owning property (fall back to a default if orphaned).
        foreach (var (baseName, named) in namedPending)
        {
            if (properties.TryGetValue(baseName, out var owner))
                owner.Named.Add(named);
        }

        foreach (var prop in properties.Values)
            prop.Named.Sort((a, b) => string.Compare(a.ConfigName, b.ConfigName, StringComparison.OrdinalIgnoreCase));

        foreach (var prop in properties.Values)
            Insert(prop);

        SortProperties(Root);
        Classes = CollectClasses(Root).OrderBy(n => n.FullPath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // {class}.{name}.{property} → {class}.{property} (drops the name segment second from the end).
    private static string? TryGetBaseName(string[] segments)
    {
        if (segments.Length < 3) return null;
        var kept = segments.Take(segments.Length - 2).Append(segments[^1]);
        return string.Join('.', kept);
    }

    private void Insert(ConfigProperty prop)
    {
        var segments = prop.ClassPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var node = Root;
        var path = string.Empty;
        foreach (var seg in segments)
        {
            path = path.Length == 0 ? seg : path + "." + seg;
            if (!node.Children.TryGetValue(seg, out var child))
            {
                child = new ConfigTreeNode { Segment = seg, FullPath = path };
                node.Children[seg] = child;
            }
            node = child;
        }
        node.Properties.Add(prop);
    }

    private static void SortProperties(ConfigTreeNode node)
    {
        node.Properties.Sort((a, b) => string.Compare(a.PropertyName, b.PropertyName, StringComparison.OrdinalIgnoreCase));
        foreach (var child in node.Children.Values)
            SortProperties(child);
    }

    private static IEnumerable<ConfigTreeNode> CollectClasses(ConfigTreeNode node)
    {
        if (node.IsClass) yield return node;
        foreach (var child in node.Children.Values)
            foreach (var descendant in CollectClasses(child))
                yield return descendant;
    }

    // ---- Write handlers -----------------------------------------------------------------------

    public class ConfigUpdateInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool IsBoolean { get; set; }
    }

    [BindProperty]
    public ConfigUpdateInput Input { get; set; } = new();

    // OnPostUpdate — invoked via fetch(). Persists a single entry (default or named alternative)
    // through POST /api/role/configurationinfo/tryupdateconfiguration and reports the outcome as
    // JSON for inline feedback. Non-success API statuses are surfaced, never turned into error pages.
    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            return new JsonResult(new { ok = false, error = "A configuration name is required." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Normalise a boolean to a canonical string the framework's bool.Parse accepts.
        var value = Input.Value;
        if (Input.IsBoolean)
            value = IsTruthy(value) ? "True" : "False";

        var payload = JsonSerializer.Serialize(new { Name = Input.Name, Value = value });

        try
        {
            var response = await _api.SendRawAsync(
                "POST", "api/role/configurationinfo/tryupdateconfiguration", payload, ct);

            if (response.IsSuccess)
                return new JsonResult(new { ok = true, value });

            return new JsonResult(new
            {
                ok = false,
                error = ExtractError(response.Body) ?? $"Update failed ({response.StatusCode} {response.ReasonPhrase})."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return new JsonResult(new { ok = false, unauthorized = true, error = "Unauthorized — your session may have expired. Please sign in again." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to update configuration {Name}", Input.Name);
            return new JsonResult(new { ok = false, error = "Request failed. Check that the Backlot API is reachable." })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }

    // OnPostRemove — remove a persisted named alternative. The Backlot API exposes no delete/remove
    // scenario for configuration entries, so per project guardrails this is left as a stub for the
    // code-behind to be implemented once such an endpoint exists.
    public IActionResult OnPostRemoveAsync()
    {
        throw new NotImplementedException(
            "Removing a persisted named configuration requires a Backlot API endpoint that does not yet exist.");
    }

    private static bool IsTruthy(string? value) =>
        value is not null &&
        (value.Equals("true", StringComparison.OrdinalIgnoreCase)
         || value.Equals("on", StringComparison.OrdinalIgnoreCase)
         || value == "1");

    // Best-effort friendly message from a Backlot error envelope: prefer Status, then a
    // ValidationOutcome's first result, else null (caller falls back to the status code).
    private static string? ExtractError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("Body", out var envBody)
                && envBody.ValueKind == JsonValueKind.Object
                && envBody.TryGetProperty("Results", out var results)
                && results.ValueKind == JsonValueKind.Array
                && results.GetArrayLength() > 0
                && results[0].TryGetProperty("ErrorMessage", out var msg)
                && msg.ValueKind == JsonValueKind.String)
            {
                return msg.GetString();
            }

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("Status", out var status)
                && status.ValueKind == JsonValueKind.String
                && !string.Equals(status.GetString(), "OK", StringComparison.OrdinalIgnoreCase))
            {
                return status.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON — nothing to extract.
        }
        return null;
    }
}
