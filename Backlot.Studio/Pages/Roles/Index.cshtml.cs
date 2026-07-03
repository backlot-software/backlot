using System.Text.Json;
using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Roles;

[Authorize]
public class IndexModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public FindResult? RoleResult { get; private set; }
    public string? ErrorMessage { get; private set; }

    [FromQuery(Name = "q")]
    public string? SearchQuery { get; set; }

    [FromQuery(Name = "page")]
    public int CurrentPage { get; set; } = 1;

    public const int PageSize = 25;

    public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();

        // Clamp page to at least 1
        if (CurrentPage < 1) CurrentPage = 1;

        // Build FindRequest: parse SearchQuery for field:value syntax
        var request = BuildFindRequest();

        try
        {
            var (result, redirect) = await SafeApiCall(async () => await _api.PlayAsync<FindResult>("simplequery", "find", request));
            if (redirect != null) return redirect;
            RoleResult = result?.Body;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load roles from Backlot API");
            ErrorMessage = "Could not load roles. Check that the Backlot API is reachable and that your credentials are valid.";
        }

        return Page();
    }

    // Helper read-only properties for the view
    public int TotalCount => RoleResult?.Total ?? 0;
    public int StartItem => RoleResult == null || RoleResult.Total == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndItem => RoleResult == null ? 0 : Math.Min(CurrentPage * PageSize, RoleResult.Total);
    public int TotalPages => RoleResult == null || RoleResult.Total == 0 ? 0 : (int)Math.Ceiling((double)RoleResult.Total / PageSize);

    /// <summary>Extracts a string field value from a dynamic role JsonElement row.</summary>
    public static string GetField(JsonElement row, string key)
    {
        if (row.TryGetProperty(key, out var v))
        {
            return v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString();
        }
        return "";
    }

    /// <summary>Returns the primary skill type (first entry in __Skills array) for a role row.</summary>
    public static string GetPrimarySkill(JsonElement row)
    {
        if (row.TryGetProperty("__Skills", out var skills) && skills.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in skills.EnumerateArray())
            {
                return element.GetString() ?? "";
            }
        }
        return "";
    }

    private FindRequest BuildFindRequest()
    {
        FindCriteria[]? criteria = null;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Regex pattern: field<operator>value
            // Matches: (\w+)([:=<>])([^\s]+)
            // - Group 1: field name (word characters: letters, digits, underscore)
            // - Group 2: operator (: = < >)
            // - Group 3: value (non-whitespace characters)
            var pattern = @"(\w+)([:=<>])([^\s]+)";
            var matches = System.Text.RegularExpressions.Regex.Matches(SearchQuery, pattern);

            if (matches.Count > 0)
            {
                criteria = new FindCriteria[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    var field = match.Groups[1].Value;
                    var op = match.Groups[2].Value;
                    var value = match.Groups[3].Value;

                    var condition = op switch
                    {
                        ":" => "ct",   // contains
                        "=" => "eq",   // equals
                        "<" => "lt",   // less than
                        ">" => "gt",   // greater than
                        _ => "ct"      // fallback
                    };

                    criteria[i] = new FindCriteria { Field = field, Condition = condition, Value = value };
                }
            }
            else
            {
                // Fallback for plain text: search Uid with contains
                criteria =
                [
                    new FindCriteria { Field = "Uid", Condition = "ct", Value = SearchQuery }
                ];
            }
        }

        return new FindRequest
        {
            Criteria = criteria,
            PageSize = PageSize,
            Page = CurrentPage
        };
    }
}
