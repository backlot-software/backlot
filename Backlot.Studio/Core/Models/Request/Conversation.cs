namespace Backlot.Studio.Core.Models.Request;

/// <summary>
/// Body and result of POST /api/role/conversation/create (Backlot.Services.AI). The scenario
/// returns the same three properties it accepts, so one class serves both directions: Message
/// carries the developer's text on the way in and the assistant's reply on the way out, and the
/// two arrays are the names agreed on so far, extended by the scenario.
/// </summary>
public class Conversation : IRequestBody
{
    public string Message { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public string[] Scenarios { get; set; } = [];
}
