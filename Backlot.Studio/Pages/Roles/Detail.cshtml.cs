using System.Text.Json;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Roles;

[Authorize]
public class DetailModel : AuthenticatedPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<DetailModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string Uid { get; set; } = string.Empty;

    // Drives the "Role saved." success banner. Set via the ?saved=1 query flag on the
    // 303 redirect target from the edit save path (D-08: query-flag mechanism, no server
    // state carried across the redirect). Binds on GET.
    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

    public JsonElement RoleData { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool CanWrite { get; private set; }

    public DetailModel(IBacklotApiClient api, ILogger<DetailModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetUserContext();

        if (string.IsNullOrWhiteSpace(Uid))
            return RedirectToPage("/Roles/Index");

        try
        {
            var (env, redirect) = await SafeApiCall(async () => await _api.PlayAsync<JsonElement>("seekbase", "detail", new { For = Uid }));
            if (redirect != null) return redirect;
            var rd = env?.Body.Unwrap("Role");

            RoleData = rd ?? new JsonElement();

            var perms = GetPermissions(RoleData);
            CanWrite = perms.CanWrite;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load role detail for uid={Uid}", Uid);
            ErrorMessage = "Could not load role details. The role may not exist or the API may be unavailable.";
        }

        return Page();
    }

    // Computed properties for the view
    public string PageTitle => GetStringField(RoleData, "Uid");
    public (bool CanCreate, bool CanRead, bool CanWrite) Permissions => GetPermissions(RoleData);
    public IEnumerable<string> Skills => GetSkills(RoleData);
    public string? LastModifiedDate => GetStringField(RoleData, "LastModified");
    public IEnumerable<(string Key, string Value)> Fields => GetNonSystemFields(RoleData);

    // Helper methods

    public static string GetStringField(JsonElement data, string key)
    {
        if (data.TryGetProperty(key, out var v))
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        return string.Empty;
    }

    public static IEnumerable<string> GetSkills(JsonElement data)
    {
        if (data.TryGetProperty("__Skills", out var skills) && skills.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in skills.EnumerateArray())
            {
                var s = element.GetString();
                if (s != null) yield return s;
            }
        }
    }

    public static (bool CanCreate, bool CanRead, bool CanWrite) GetPermissions(JsonElement data)
    {
        bool canCreate = false, canRead = false, canWrite = false;
        if (data.TryGetProperty("__Permission", out var perm) && perm.ValueKind == JsonValueKind.Object)
        {
            if (perm.TryGetProperty("CanCreate", out var cc) && cc.ValueKind == JsonValueKind.True)
                canCreate = true;
            if (perm.TryGetProperty("CanRead", out var cr) && cr.ValueKind == JsonValueKind.True)
                canRead = true;
            if (perm.TryGetProperty("CanWrite", out var cw) && cw.ValueKind == JsonValueKind.True)
                canWrite = true;
        }
        return (canCreate, canRead, canWrite);
    }

    public static IEnumerable<(string Key, string Value)> GetNonSystemFields(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) yield break;
        var props = data.EnumerateObject()
            .Where(p => !p.Name.StartsWith("__", StringComparison.Ordinal))
            .ToList();
        var ordered = props
            .Where(p => p.Name != "Uid" && p.Name != "LastModified")
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
        
        var uid = props.FirstOrDefault(p => p.Name == "Uid");
        var lastModified = props.FirstOrDefault(p => p.Name == "LastModified");
        
        var result = new List<JsonProperty>();
        
        if (uid.Value.ValueKind != JsonValueKind.Undefined) 
            result.Add(uid);
        
        result.AddRange(ordered);
        
        if (lastModified.Value.ValueKind != JsonValueKind.Undefined) result.Add(lastModified);
        
        foreach (var prop in result)
        {
            var val = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? ""
                : prop.Value.ToString();
            yield return (prop.Name, val);
        }
    }
}
