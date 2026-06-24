using System.Globalization;
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

            // UX-only pre-check (T-04-02, WR-03): short-circuit before isvalid/persist when the
            // detail fetch shows no write permission, so the user gets immediate feedback rather
            // than a round-trip rejection. This is NOT the security boundary — the Backlot API
            // enforces IPermission authoritatively, and a 401/403 from persist is handled in the
            // catch below in case permissions change between this fetch and the write (TOCTOU).
            if (!CanWrite)
            {
                ErrorMessage = "You don't have permission to edit this role.";
                return TurboInvalidPage();
            }

            var payload = BuildPayload(Schema.Fields);

            var (outcome, r2) = await SafeApiCall(async () => await _api.ValidateRoleAsync(payload));
            if (r2 != null) return r2;

            if (outcome is null || !outcome.IsValid)
            {
                // Defensive parse (RESEARCH Q2): Body is typed bare object and "can change
                // without notice". When invalid but Results is empty/missing, surface a single
                // generic item so the summary block is never blank. Null ErrorMessage items are
                // tolerated by the view (`e.ErrorMessage ?? "Validation failed."`).
                var results = outcome?.Results ?? [];
                ValidationErrors = results.Count > 0
                    ? results
                    : [new ValidationResultItem { ErrorMessage = "Validation failed." }];
                return TurboInvalidPage(); // 422
            }

            var (_, r3) = await SafeApiCall(async () => await _api.PersistRoleAsync(payload));
            if (r3 != null) return r3;

            // Encode the user-supplied Uid before placing it in the Location header so special
            // URL characters can't produce a malformed redirect target or header injection
            // (WR-01). Url.Page builds a framework-encoded path; fall back to a manually escaped
            // segment if the page route can't be resolved.
            var location = Url.Page("/Roles/Detail", new { uid = Uid, saved = 1 })
                ?? $"/roles/{Uri.EscapeDataString(Uid)}?saved=1";
            return TurboRedirect(location); // 303
        }
        catch (BacklotApiException ex) when (
            ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized)
        {
            // The API is the authoritative permission boundary (WR-03): the local CanWrite gate
            // above is UX-only. If permissions changed between the detail fetch and persist, the
            // API rejects the write — surface that explicitly instead of a generic save failure.
            _logger.LogWarning(ex, "Permission denied saving role uid={Uid} (status {Status})", Uid, ex.StatusCode);
            ErrorMessage = "You don't have permission to edit this role.";
            return TurboInvalidPage();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to save role uid={Uid}", Uid);
            ErrorMessage = "Save failed — the change was not stored. Your entries are preserved; try again.";
            return TurboInvalidPage();
        }
    }

    // Match the schema row to the role by the role's OWN/most-derived skill (EDIT-01).
    // __Skills is built from Type.GetInterfaces() (Backlot.Core/Loader.cs:280-296), which lists
    // the inherited base markers (Persist, Permission, Role, Uid) FIRST and appends the role's
    // own concrete name LAST. The director/roles schema rows are keyed by the concrete role name
    // (role.GetRoleName(), e.g. "Message"). We therefore walk __Skills from most-derived (last)
    // to least-derived (first) and return the first schema row whose Role matches a skill
    // (case-insensitive). Returns null only when NO skill matches any schema row — the caller
    // treats null as the explicit "no schema" state and never edits a guessed field set.
    //
    // WR-06 reconciliation: WR-06 ("match the primary skill == __Skills[0] ONLY; do not fall
    // back to secondary skills") was written on the FALSE premise that __Skills[0] is the role's
    // own type. __Skills[0] is actually a base marker (e.g. "Persist"), so the old match never
    // resolved the concrete role row and the form rendered ZERO fields. WR-06's underlying intent
    // — bind exactly ONE deterministic schema row and never edit fields under the wrong role
    // contract — is preserved here: this logic still resolves to a single row, PREFERS the role's
    // own/most-derived contract, and returns null when nothing matches (no guessing).
    private static RoleSchema? MatchSchema(System.Text.Json.JsonElement detail, IReadOnlyList<RoleSchema> schemas)
    {
        var skills = DetailModel.GetSkills(detail).ToList();
        for (var i = skills.Count - 1; i >= 0; i--)
        {
            var match = schemas.FirstOrDefault(r => string.Equals(r.Role, skills[i], StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }
        return null;
    }

    // Build the persist/isvalid payload from the SCHEMA field list (never the posted keys).
    // Skip Calculated/read-only fields; default missing bool fields to false (Pitfall 3).
    // T-04-03: only schema-known fields plus the hidden Uid are forwarded, so arbitrary
    // posted keys cannot be injected.
    private Dictionary<string, object?> BuildPayload(IReadOnlyList<FieldSchema> schema)
    {
        var payload = new Dictionary<string, object?> { ["Uid"] = Uid };
        foreach (var f in schema)
        {
            if (IsReadOnly(f)) continue;
            Fields.TryGetValue(f.Field, out var raw);
            payload[f.Field] = CoerceByType(f.Type, raw);
        }
        return payload;
    }

    // Coerce a posted string to the schema Type. Bool defaults to false when the checkbox
    // posts nothing (Pitfall 3 — unchecked boxes send no key). Numerics parse to the matching
    // CLR type; on parse failure the raw string is returned unchanged so the API surfaces a
    // validation error rather than the form crashing. Everything else passes through as-is.
    private static object? CoerceByType(string type, string? raw)
    {
        if (IsBool(type))
            return raw == "true";

        if (!IsNumeric(type) || string.IsNullOrWhiteSpace(raw))
            return raw;

        // Parse with InvariantCulture so the `.` decimal separator from HTML number inputs and
        // the round-trip from CurrentValue is interpreted consistently regardless of the host's
        // configured culture (CR-01: a comma-decimal locale would otherwise corrupt "1.5" → 15).
        var inv = CultureInfo.InvariantCulture;
        return type switch
        {
            "Byte" => byte.TryParse(raw, NumberStyles.Integer, inv, out var b) ? b : (object?)raw,
            "SByte" => sbyte.TryParse(raw, NumberStyles.Integer, inv, out var sb) ? sb : (object?)raw,
            "Int16" => short.TryParse(raw, NumberStyles.Integer, inv, out var s) ? s : (object?)raw,
            "UInt16" => ushort.TryParse(raw, NumberStyles.Integer, inv, out var us) ? us : (object?)raw,
            "Int32" => int.TryParse(raw, NumberStyles.Integer, inv, out var i) ? i : (object?)raw,
            "UInt32" => uint.TryParse(raw, NumberStyles.Integer, inv, out var ui) ? ui : (object?)raw,
            "Int64" => long.TryParse(raw, NumberStyles.Integer, inv, out var l) ? l : (object?)raw,
            "UInt64" => ulong.TryParse(raw, NumberStyles.Integer, inv, out var ul) ? ul : (object?)raw,
            "Decimal" => decimal.TryParse(raw, NumberStyles.Number, inv, out var dec) ? dec : (object?)raw,
            "Double" => double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, inv, out var d) ? d : (object?)raw,
            "Single" => float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, inv, out var fl) ? fl : (object?)raw,
            _ => raw
        };
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
