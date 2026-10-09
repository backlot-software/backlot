namespace Backlot.Studio.Areas.Studio.Pages.ViewModels;

/// <summary>
/// The Development tool's conversation, held in the Studio session so it survives navigating to
/// another page and back. CodeGen's <c>IChat</c> role expects the whole message history on every
/// request, so the API-shaped messages are retained beside the display-friendly transcript.
/// </summary>
public class DevelopmentConversation
{
    /// <summary>Session key the state is stored under.</summary>
    public const string SessionKey = "Development.Conversation";

    /// <summary>Studio's own opening line; it is never fetched from the API.</summary>
    public const string OpeningQuestion = "Describe a scenario you like to create";

    public List<ChatTurn> Turns { get; set; } = [];
    public List<DevelopmentMessage> Messages { get; set; } = [];

    public static DevelopmentConversation Opening() => new()
    {
        Turns = [new ChatTurn { FromUser = false, Text = OpeningQuestion }]
    };
}

public class ChatTurn
{
    /// <summary>True for the developer's own messages, false for the assistant's.</summary>
    public bool FromUser { get; set; }

    public string Text { get; set; } = string.Empty;
}

/// <summary>One message in the <c>IChat.Messages</c> request payload.</summary>
public class DevelopmentMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
