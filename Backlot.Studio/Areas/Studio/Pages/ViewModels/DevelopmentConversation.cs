namespace Backlot.Studio.Areas.Studio.Pages.ViewModels;

/// <summary>
/// The Development tool's conversation, held in the Studio session so it survives navigating to
/// another page and back. The transcript is kept for display only — the create scenario's wire
/// contract carries no history, so only the newest message plus the two name lists are ever sent.
/// </summary>
public class DevelopmentConversation
{
    /// <summary>Session key the state is stored under.</summary>
    public const string SessionKey = "Development.Conversation";

    /// <summary>Studio's own opening line; it is never fetched from the API.</summary>
    public const string OpeningQuestion = "Describe a scenario you like to create";

    public List<ChatTurn> Turns { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public List<string> Scenarios { get; set; } = [];

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
