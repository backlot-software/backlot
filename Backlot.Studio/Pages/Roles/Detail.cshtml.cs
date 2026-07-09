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

    public class FieldViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsJson { get; set; }
        public string? JsonType { get; set; }
        public string? OneLinePreview { get; set; }
    }

    // Raw .http request text for POST /api/role/{RoleType}/persist, copied to the
    // clipboard from the detail page. Empty when the role couldn't be loaded.
    public string HttpRequestText { get; private set; } = string.Empty;

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

            if (RoleData.ValueKind == JsonValueKind.Object)
                HttpRequestText = BuildHttpRequest(RoleData);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load role detail for uid={Uid}", Uid);
            ErrorMessage = "Could not load role details. The role may not exist or the API may be unavailable.";
        }

        return Page();
    }

    // Builds the raw .http request text for POST /api/role/{RoleType}/persist with the
    // role's current fields. The Authorization line carries the same base64 credential
    // the app uses for its own API requests, read from session ("BasicAuthHeader", stored
    // without the "Basic " prefix by Login.cshtml.cs). Missing session value → empty.
    private string BuildHttpRequest(JsonElement roleData)
    {
        var baseUrl = _api.BaseUrl.ToString().TrimEnd('/');
        var authHeader = HttpContext?.Session.GetString("BasicAuthHeader") ?? string.Empty;
        var body = BuildBody(roleData);

        var sb = new System.Text.StringBuilder();
        sb.Append("POST ").Append(baseUrl).Append("/api/role/").Append(RoleType).Append("/persist").Append('\n');
        sb.Append("Content-Type: application/json").Append('\n');
        sb.Append("Authorization: Basic ").Append(authHeader).Append('\n');
        sb.Append('\n');
        sb.Append(body).Append('\n');
        return sb.ToString();
    }

    // Serializes the role's non-system fields (Uid first) into a pretty-printed JSON object.
    // JsonObject preserves insertion order; values are emitted as strings, which is acceptable
    // for a hand-editable template. Never throws on malformed data (T-lg9-02).
    private static string BuildBody(JsonElement roleData)
    {
        if (roleData.ValueKind != JsonValueKind.Object)
            return "{}";

        var obj = new JsonObject();

        // Uid first
        if (roleData.TryGetProperty("Uid", out var uid))
        {
            obj["Uid"] = JsonSerializer.SerializeToNode(uid);
        }

        // Other non-system fields in order
        var otherProps = roleData.EnumerateObject()
            .Where(p => !p.Name.StartsWith("__", StringComparison.Ordinal)
                        && p.Name != "Uid"
                        && p.Name != "LastModified")
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var prop in otherProps)
        {
            obj[prop.Name] = JsonSerializer.SerializeToNode(prop.Value);
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
    public IEnumerable<FieldViewModel> Fields => GetNonSystemFields(RoleData);

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

    public static IEnumerable<FieldViewModel> GetNonSystemFields(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) yield break;
        var props = data.EnumerateObject()
            .Where(p => !p.Name.StartsWith("__", StringComparison.Ordinal))
            .ToList();
        var ordered = props
            .Where(p => p.Name != "Uid" && p.Name != "LastModified")
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
        
        var uid = props.FirstOrDefault(p => p.Name == "Uid");
        
        var result = new List<JsonProperty>();
        
        if (uid.Value.ValueKind != JsonValueKind.Undefined) 
            result.Add(uid);
        
        result.AddRange(ordered);
        
        foreach (var prop in result)
        {
            string value;
            bool isJson = false;
            string? jsonType = null;

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    value = prop.Value.GetString() ?? "";
                    break;

                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    value = FormatJsonValue(prop.Value);
                    isJson = true;
                    jsonType = prop.Value.ValueKind.ToString().ToLower();
                    break;

                default:
                    value = prop.Value.ToString();
                    break;
            }

            yield return new FieldViewModel
            {
                Key = prop.Name,
                Value = value,
                IsJson = isJson,
                JsonType = jsonType,
                OneLinePreview = isJson ? OneLineJsonView(prop.Value) : null
            };
        }
    }

    private static string FormatJsonValue(JsonElement element)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var node = JsonSerializer.SerializeToNode(element, options);
        return node?.ToJsonString(options) ?? element.ToString();
    }

    private static string OneLineJsonView(JsonElement element)
    {
        var values = new List<string>();
        CollectJsonValues(element, values);
        return string.Join(", ", values);
    }

    private static void CollectJsonValues(JsonElement element, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    CollectJsonValues(prop.Value, values);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectJsonValues(item, values);
                }
                break;

            case JsonValueKind.String:
                values.Add(element.GetString() ?? "");
                break;

            case JsonValueKind.Number:
                values.Add(element.ToString());
                break;

            case JsonValueKind.True:
                values.Add("true");
                break;

            case JsonValueKind.False:
                values.Add("false");
                break;

            case JsonValueKind.Null:
                values.Add("null");
                break;

            default:
                values.Add(element.ToString());
                break;
        }
    }
}
