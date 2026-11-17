using Backlot.Authentication.Razor.Core.Models.Response;

namespace Backlot.Authentication.Razor.Core.Models.Request;

/// <summary>POST body for POST /api/role/simplequery/find.</summary>
public class FindRequest : IRequestBody
{
    public string For { get; set; } = "Persist";
    public FindCriteria[]? Criteria { get; set; }
    public int PageSize { get; set; }
    public int Page { get; set; }
    
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? Till { get; set; } 
}