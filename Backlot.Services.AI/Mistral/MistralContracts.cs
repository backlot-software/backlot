using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global : deserialized by System.Text.Json.
// ReSharper disable UnusedAutoPropertyAccessor.Global : used by the external api.
namespace Backlot.Services.AI.Mistral;

/// <summary>Request body for the Mistral chat-completions endpoint.</summary>
internal class ChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("messages")] public ChatMessage[] Messages { get; set; }

    /// <summary>
    /// Forces the model to answer with a JSON object so the reply can be parsed instead of scraped
    /// out of prose.
    /// </summary>
    [JsonPropertyName("response_format")] public ResponseFormat ResponseFormat { get; set; }
}

internal class ResponseFormat
{
    [JsonPropertyName("type")] public string Type { get; set; }
}

internal class ChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; }
}

/// <summary>Response body of the Mistral chat-completions endpoint (only what is used).</summary>
internal class ChatResponse
{
    [JsonPropertyName("choices")] public Choice[] Choices { get; set; }
}

internal class Choice
{
    [JsonPropertyName("message")] public ChatMessage Message { get; set; }
}

/// <summary>
/// The JSON the model is instructed to put inside its message content. Mirrors
/// <c>IConversation</c> one to one.
/// </summary>
internal class ConversationReply
{
    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("roles")] public string[] Roles { get; set; }

    [JsonPropertyName("scenarios")] public string[] Scenarios { get; set; }
}
