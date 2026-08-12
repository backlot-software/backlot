namespace Backlot.Studio.Models.Api;

public class Seek : IRequestBody
{
    public required string For { get; set; }
}