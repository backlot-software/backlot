using System.Text.Json;

namespace Backlot.Studio.Models.Api;

/// <summary>Represents a single filter criterion for the simplequery/find endpoint.</summary>
public class FindCriteria
{
    public string Field { get; set; } = "";
    public string Condition { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>POST body for POST /api/role/simplequery/find.</summary>
public class FindRequest
{
    public string For { get; set; } = "Persist";
    public FindCriteria[]? Criteria { get; set; }
    public int PageSize { get; set; }
    public int Page { get; set; }
}

/// <summary>
/// Deserialized body of the ApiEnvelope returned by simplequery/find.
/// Results is typed as JsonElement[] because each role object has a dynamic schema.
/// </summary>
public class FindResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public JsonElement[] Results { get; set; } = [];
}

/// <summary>One entry from the persist/relations response array.</summary>
public class RelationItem
{
    public string Uid { get; set; } = "";
    public string Info { get; set; } = "";
}
