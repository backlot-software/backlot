using Backlot.Studio.Models.Api;
using Backlot.Studio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Pages.Roles;

[Authorize]
public class EditModel : TurboEditPageModel
{
    private readonly IBacklotApiClient _api;
    private readonly ILogger<EditModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string Uid { get; set; } = string.Empty;

    // Posted field values, bound from name="Fields[FieldName]".
    [BindProperty]
    public Dictionary<string, string?> Fields { get; set; } = new();

    public RoleSchema? Schema { get; private set; }
    public IReadOnlyList<FieldSchema> SchemaFields => Schema?.Fields ?? [];
    public string PageTitle { get; private set; } = "Edit Role";
    public bool CanWrite { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<ValidationResultItem> ValidationErrors { get; private set; } = [];

    public EditModel(IBacklotApiClient api, ILogger<EditModel> logger)
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
            var (detail, r1) = await SafeApiCall(async () => await _api.GetRoleDetailAsync(Uid));
            if (r1 != null) return r1;

            var (schema, r2) = await SafeApiCall(async () => await _api.GetRoleSchemaAsync());
            if (r2 != null) return r2;

            if (detail.HasValue && schema != null)
            {
                Schema = MatchSchema(detail.Value, schema);
                PageTitle = $"Edit {DetailModel.GetPageTitle(detail.Value)}";
                CanWrite = DetailModel.GetPermissions(detail.Value).CanWrite;

                if (Schema != null)
                {
                    // Seed the field dictionary with current values from seekbase/detail.
                    foreach (var f in Schema.Fields)
                        Fields[f.Field] = DetailModel.GetStringField(detail.Value, f.Field);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load role for editing uid={Uid}", Uid);
            ErrorMessage = "Couldn't load this role for editing.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetUserContext();

        try
        {
            // Re-fetch the schema: it is the authoritative field list. Never trust the
            // posted keys (mass-assignment / field-injection guard, T-04-03 / RESEARCH Q5).
            var (schema, r0) = await SafeApiCall(async () => await _api.GetRoleSchemaAsync());
            if (r0 != null) return r0;

            // Match by the posted skill carried via the schema; fall back to re-deriving
            // from a detail fetch is unnecessary here because the schema row is identified
            // by the same set of fields the form rendered. Re-resolve from the role detail
            // so the schema row is matched the same way as on GET.
            var (detail, r1) = await SafeApiCall(async () => await _api.GetRoleDetailAsync(Uid));
            if (r1 != null) return r1;

            if (schema == null || !detail.HasValue)
            {
                ErrorMessage = "Couldn't load this role for editing.";
                return TurboInvalidPage();
            }

            Schema = MatchSchema(detail.Value, schema);
            PageTitle = $"Edit {DetailModel.GetPageTitle(detail.Value)}";
            CanWrite = DetailModel.GetPermissions(detail.Value).CanWrite;

            if (Schema == null)
            {
                ErrorMessage = "Couldn't load this role for editing.";
                return TurboInvalidPage();
            }

            var payload = BuildPayload(Schema.Fields);

            var (outcome, r2) = await SafeApiCall(async () => await _api.ValidateRoleAsync(payload));
            if (r2 != null) return r2;

            if (outcome is null || !outcome.IsValid)
            {
                ValidationErrors = outcome?.Results ?? [];
                return TurboInvalidPage(); // 422
            }

            var (_, r3) = await SafeApiCall(async () => await _api.PersistRoleAsync(payload));
            if (r3 != null) return r3;

            return TurboRedirect($"/roles/{Uid}?saved=1"); // 303
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to save role uid={Uid}", Uid);
            ErrorMessage = "Save failed — the change was not stored. Your entries are preserved; try again.";
            return TurboInvalidPage();
        }
    }

    // Match the schema row to the role by its primary skill (__Skills[0] == schema.Role,
    // RESEARCH Pattern 3). Fall back to the first __Skills entry that matches any schema.Role.
    private static RoleSchema? MatchSchema(System.Text.Json.JsonElement detail, IReadOnlyList<RoleSchema> schemas)
    {
        var skills = DetailModel.GetSkills(detail).ToList();
        var primary = skills.FirstOrDefault();
        if (primary != null)
        {
            var match = schemas.FirstOrDefault(r => string.Equals(r.Role, primary, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        // Fallback: first skill that matches any schema row.
        foreach (var skill in skills)
        {
            var match = schemas.FirstOrDefault(r => string.Equals(r.Role, skill, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        return null;
    }

    // Build the persist/isvalid payload from the SCHEMA field list (never the posted keys).
    // Skip Calculated/read-only fields; default missing bool fields to false (Pitfall 3).
    private Dictionary<string, object?> BuildPayload(IReadOnlyList<FieldSchema> schema)
    {
        var payload = new Dictionary<string, object?> { ["Uid"] = Uid };
        foreach (var f in schema)
        {
            if (IsReadOnly(f)) continue;
            Fields.TryGetValue(f.Field, out var raw);
            payload[f.Field] = IsBool(f.Type) ? raw == "true" : raw;
        }
        return payload;
    }

    // View helpers — read-only iff the field carries the Calculated characteristic exactly
    // (RESEARCH Q3 / Pitfall 4: do NOT treat Required/StringLength/Range as read-only).
    public static bool IsReadOnly(FieldSchema f) =>
        f.Characteristics.Any(c => string.Equals(c.Characteristic, "Calculated", StringComparison.Ordinal));

    public static bool IsBool(string type) =>
        string.Equals(type, "Boolean", StringComparison.Ordinal);

    public static bool IsNumeric(string type) => type switch
    {
        "Int32" or "Int64" or "Int16" or "Byte" or "SByte"
        or "UInt16" or "UInt32" or "UInt64"
        or "Decimal" or "Double" or "Single" => true,
        _ => false
    };

    public string? CurrentValue(string field) =>
        Fields.TryGetValue(field, out var v) ? v : null;
}
