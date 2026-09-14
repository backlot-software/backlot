using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backlot.Http;

namespace Backlot.Services.AI.Mistral;

/// <summary>
/// Thin client over the Mistral chat-completions API. It owns exactly one exchange: send the
/// system prompt plus the current turn, get a <see cref="ConversationReply"/> back.
///
/// The HttpClient comes from IHttpClientFactory rather than "new HttpClient()" - the factory is
/// registered by ConfigureBacklotWeb, so nothing has to be wired up in a host's Director, and it
/// is also the seam tests substitute.
/// </summary>
internal class MistralChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _clients;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _model;

    /// <summary>
    /// Blank settings fall back to the documented defaults, and a blank ApiKey falls back to the
    /// MISTRAL_API_KEY environment variable, so a live key never has to be committed to the
    /// tracked {Environment}.jsonsettings.json.
    /// </summary>
    public MistralChatClient(IHttpClientFactory clients, string apiKey, string endpoint, string model)
    {
        _clients = clients;
        _apiKey = Coalesce(apiKey, Environment.GetEnvironmentVariable(Defaults.ApiKeyEnvironmentVariable));
        _endpoint = Coalesce(endpoint, Defaults.Endpoint);
        _model = Coalesce(model, Defaults.Model);
    }

    private static string Coalesce(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public async Task<ConversationReply> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new BadRequestException(
                "Backlot.Services.AI is not configured: set Backlot > Services > AI > Settings > ApiKey " +
                $"or the {Defaults.ApiKeyEnvironmentVariable} environment variable.");
        }

        var payload = new ChatRequest
        {
            Model = _model,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userPrompt }
            ]
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _clients.CreateClient(nameof(MistralChatClient));
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // Surface the upstream status to the operator instead of letting it become an
            // anonymous 500. The body is not echoed back: it can carry request details.
            throw new BadRequestException(
                $"The AI endpoint answered {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        return Parse(body);
    }

    /// <summary>
    /// Unwraps the completion and parses the JSON object the model was told to produce. Any shape
    /// that is not the agreed contract becomes a BadRequestException so the operator sees "the
    /// model answered in the wrong shape" rather than a logged 500 with a GUID.
    /// </summary>
    private static ConversationReply Parse(string body)
    {
        ChatResponse completion;
        try
        {
            completion = JsonSerializer.Deserialize<ChatResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            throw new BadRequestException("The AI endpoint returned a response that could not be read.");
        }

        var content = completion?.Choices is { Length: > 0 } choices ? choices[0].Message?.Content : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new BadRequestException("The AI endpoint returned an empty response.");
        }

        ConversationReply reply;
        try
        {
            reply = JsonSerializer.Deserialize<ConversationReply>(content, JsonOptions);
        }
        catch (JsonException)
        {
            throw new BadRequestException("The AI model did not answer with the expected JSON object.");
        }

        if (reply == null || string.IsNullOrWhiteSpace(reply.Message))
        {
            throw new BadRequestException("The AI model did not answer with the expected JSON object.");
        }

        reply.Roles ??= [];
        reply.Scenarios ??= [];

        return reply;
    }

    /// <summary>
    /// The system prompt. It has to carry the Movie Pattern Practice vocabulary, because the whole
    /// point of the conversation is to land on Backlot roles and scenarios - and it has to pin the
    /// reply shape, because the scenario contract is names only.
    /// </summary>
    public static string SystemPrompt =>
        """
        You are a design assistant for Backlot, a .NET framework that structures applications with
        the Movie Pattern Practice:

        - A ROLE is an interface describing WHAT something can do. Role names start with "I",
          e.g. "IProduct", "IOrder", "IBasket".
        - A SCENARIO is a class describing WHAT THE CODE DOES. It has exactly one main role and
          returns a result. Scenario names are verbs or verb phrases, e.g. "Checkout",
          "AddToBasket", "CancelOrder".

        You are talking to a developer who is designing an application. Ask one focused follow-up
        question at a time until the design is clear, and keep proposing the roles and scenarios
        the conversation implies.

        Answer with a JSON object and nothing else:

        {
          "message": "your reply or next question, plain text",
          "roles": ["IProduct", "IOrder"],
          "scenarios": ["Checkout", "AddToBasket"]
        }

        Rules:
        - "roles" and "scenarios" are NAMES ONLY. No source code, no descriptions, no properties.
        - Always return the complete lists agreed so far, including names from earlier turns.
        - Never remove a name the developer has not asked you to remove.
        - Return empty arrays if nothing has been agreed yet.
        """;

    /// <summary>
    /// The user turn: what the developer said, plus the state the conversation has accumulated.
    /// This is the only context the model gets - the wire contract carries no transcript.
    /// </summary>
    public static string BuildUserPrompt(string message, IEnumerable<string> roles, IEnumerable<string> scenarios)
    {
        var sb = new StringBuilder();
        sb.Append("Developer says: ").AppendLine(message);
        sb.AppendLine();
        sb.Append("Roles agreed so far: ").AppendLine(Join(roles));
        sb.Append("Scenarios agreed so far: ").AppendLine(Join(scenarios));
        return sb.ToString();
    }

    private static string Join(IEnumerable<string> values)
    {
        var joined = string.Join(", ", values ?? []);
        return string.IsNullOrWhiteSpace(joined) ? "(none yet)" : joined;
    }
}
