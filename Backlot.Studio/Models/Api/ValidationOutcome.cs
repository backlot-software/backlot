namespace Backlot.Studio.Models.Api;

/// <summary>
/// Deserialized Body of POST /api/role/role/isvalid.
/// Shape (from framework source IsValid.Exec): { IsValid: bool, Results: ICollection&lt;ValidationResult&gt; }.
/// Parse defensively — the API doc types Body as bare object and the framework comment
/// notes the shape "can change without notice". Confirm PascalCase casing on first live run (A1).
/// </summary>
public class ValidationOutcome
{
    public bool IsValid { get; set; }
    public List<ValidationResultItem> Results { get; set; } = [];
}

/// <summary>
/// One System.ComponentModel.DataAnnotations.ValidationResult.
/// MemberNames is captured but unused in v1 (D-07 summary block); reserved for the v2
/// inline-per-field upgrade (EDIT-02 / ADV).
/// </summary>
public class ValidationResultItem
{
    public string? ErrorMessage { get; set; }
    public List<string>? MemberNames { get; set; }
}
