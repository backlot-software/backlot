namespace Backlot.Studio.Core.Models.Request;

/// <summary>
/// Wire contract for CodeGen's <c>IChat</c> role.  CodeGen keeps the complete
/// conversation on the role, so every request contains the accumulated history.
/// </summary>
public class Chat : IRequestBody
{
    public List<ChatMessage> Messages { get; set; } = [];
}

/// <summary>One CodeGen <c>IChatMessage</c> value.</summary>
public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
