namespace Backlot.Authentication.Razor.Core.Models.Request;

/// <summary>POST body for POST /api/role/simplequery/find.</summary>
public class Seek : IRequestBody
{
    public required string For { get; set; }
}