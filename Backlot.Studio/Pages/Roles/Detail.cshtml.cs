using System.Text.Json;
using System.Text.Json.Nodes;
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
    
    [BindProperty(SupportsGet = true)]
    public string RoleType { get; set; } = "Persist";

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
            return RedirectToPage("/Roles/Persist");

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

    // Downloads a ready-to-edit .http request template (VS Code REST Client / Rider format)
    // for POST /api/role/{RoleType}/persist, pre-filled with the role's current fields.
    // Reachable via ?handler=Download on the existing /roles/{roletype}/{uid} route.
    // Re-fetches the role because handler invocations do not share the GET-populated RoleData.
    public async Task<IActionResult> OnGetDownloadAsync()
    {
        SetUserContext();

        if (string.IsNullOrWhiteSpace(Uid))
            return NotFound();

        try
        {
            var (env, redirect) = await SafeApiCall(async () => await _api.PlayAsync<JsonElement>("seekbase", "detail", new { For = Uid }));
            if (redirect != null) return redirect;
            var rd = env?.Body.Unwrap("Role");

            if (rd is null || rd.Value.ValueKind != JsonValueKind.Object)
                return NotFound();

            var content = BuildHttpRequest(rd.Value);
            var fileName = BuildFileName();
            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/plain", fileName);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to build .http download for uid={Uid}", Uid);
            return NotFound();
        }
    }

    // Builds the raw .http request text. The Authorization line carries only a static
    // placeholder — session credentials are never read or embedded (T-lg9-01: credentials
    // never reach the browser per the project security boundary).
    private string BuildHttpRequest(JsonElement roleData)
    {
        var baseUrl = _api.BaseUrl.ToString().TrimEnd('/');
        var body = BuildBody(roleData);

        var sb = new System.Text.StringBuilder();
        sb.Append("@baseUrl = ").Append(baseUrl).Append('\n');
        sb.Append('\n');
        sb.Append("POST {{baseUrl}}/api/role/").Append(RoleType).Append("/persist").Append('\n');
        sb.Append("Content-Type: application/json").Append('\n');
        sb.Append("# Replace the placeholder below with the base64 of your own \"username:password\".").Append('\n');
        sb.Append("# Backlot.Studio never embeds credentials in this file.").Append('\n');
        sb.Append("Authorization: Basic <base64 of username:password>").Append('\n');
        sb.Append('\n');
        sb.Append(body).Append('\n');
        return sb.ToString();
    }

    // Serializes the role's non-system fields (Uid first) into a pretty-printed JSON object.
    // JsonObject preserves insertion order; values are emitted as strings, which is acceptable
    // for a hand-editable template. Never throws on malformed data (T-lg9-02).
    private static string BuildBody(JsonElement roleData)
    {
        var obj = new JsonObject();
        foreach (var (key, value) in GetNonSystemFields(roleData))
        {
            obj[key] = value;
        }
        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    // Produces a safe download file name: {RoleType}-{Uid}.http with invalid path chars replaced.
    private string BuildFileName()
    {
        var raw = $"{RoleType}-{Uid}.http";
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c, '_');
        }
        return raw;
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
