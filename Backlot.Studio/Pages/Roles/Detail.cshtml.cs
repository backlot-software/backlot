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

    public JsonElement? RoleData { get; private set; }
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
            RoleData = env is null ? null : BacklotApiClient.UnwrapRoleDetail(env.Body);

            if (RoleData.HasValue)
            {
                var perms = GetPermissions(RoleData.Value);
                CanWrite = perms.CanWrite;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load role detail for uid={Uid}", Uid);
            ErrorMessage = "Could not load role details. The role may not exist or the API may be unavailable.";
        }

        return Page();
    }

    // Computed properties for the view
    public string PageTitle => RoleData.HasValue ? GetPageTitle(RoleData.Value) : "Role";
    public (bool CanCreate, bool CanRead, bool CanWrite) Permissions => RoleData.HasValue ? GetPermissions(RoleData.Value) : (false, false, false);
    public IEnumerable<string> Skills => RoleData.HasValue ? GetSkills(RoleData.Value) : [];
    public string? LastModifiedDate => RoleData.HasValue ? GetStringField(RoleData.Value, "__LastModifiedDate") : null;
    public IEnumerable<(string Key, string Value)> Fields => RoleData.HasValue ? GetNonSystemFields(RoleData.Value) : [];

    // Helper methods

    public static string? GetStringField(JsonElement data, string key)
    {
        if (data.TryGetProperty(key, out var v))
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        return null;
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
        foreach (var prop in data.EnumerateObject())
        {
            if (prop.Name.StartsWith("__", StringComparison.Ordinal)) continue;
            var val = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? ""
                : prop.Value.ToString();
            yield return (prop.Name, val);
        }
    }

    // The role's own/most-derived skill is the LAST element of __Skills (Type.GetInterfaces()
    // appends the role's own concrete name last; inherited base markers like Persist/Permission
    // come first). Use LastOrDefault() so the page title / "Edit {title}" header shows the
    // concrete role name (e.g. "Message"), not a base marker (e.g. "Persist"). This shares the
    // same most-derived-skill selection as MatchSchema in Edit.cshtml.cs.
    public static string GetPageTitle(JsonElement data)
    {
        return GetSkills(data).LastOrDefault() ?? "Role";
    }
}
