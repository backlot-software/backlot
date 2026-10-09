#if false // Superseded by the focused Roles, Scenarios, and Services source files.
using System.Net.Http.Json;
using System.Text.Json;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Demo.Web;

/// <summary>The chat role consumed by Backlot Studio's Development screen.</summary>
public interface IChat : IRole
{
    List<IChatMessage> Messages { get; set; }
}

/// <summary>One message in the conversation submitted to the CodeGen agent.</summary>
public interface IChatMessage : IRole
{
    string Role { get; set; }
    string Content { get; set; }
}

/// <summary>
/// Routes a chat turn to the same Mistral agents used by CodeGen. The Studio sends the complete
/// transcript, allowing the agent to retain conversational context without Studio holding secrets.
/// </summary>
[Scenario(typeof(HandleMessage), access: [Access.Open])]
public class HandleMessage(IChat role, ICodeGenAgentService agents) : Scenario<IChat, string>(role)
{
    protected override async Task<string> ExecAsync()
    {
        var latest = Role.Messages?.LastOrDefault()?.Content ?? string.Empty;
        var agent = latest.Contains("extract roles", StringComparison.OrdinalIgnoreCase)
            ? CodeGenAgent.RoleExtraction
            : latest.Contains("generate lowcode", StringComparison.OrdinalIgnoreCase)
                ? CodeGenAgent.LowCode
                : CodeGenAgent.Chat;

        return await agents.SendAsync(Role.Messages ?? [], agent);
    }
}

public interface ICodeGenAgentService
{
    Task<string> SendAsync(IReadOnlyCollection<IChatMessage> messages, CodeGenAgent agent);
}

public enum CodeGenAgent
{
    Chat,
    RoleExtraction,
    LowCode
}

/// <summary>
/// Server-side client for the Mistral agent endpoint. Values can be supplied either through the
/// CodeGen configuration section or through the environment variables used by the original app.
/// </summary>
public class CodeGenAgentService(HttpClient httpClient, IConfiguration configuration) : ICodeGenAgentService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> SendAsync(IReadOnlyCollection<IChatMessage> messages, CodeGenAgent agent)
    {
        var baseUrl = Value("BaseUrl", "BASE_URL");
        var apiKey = Value("ApiKey", "API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? string.Empty;
        }

        var agentVersion = int.TryParse(_configuration["CodeGen:AgentVersion"], out var configuredVersion)
            ? configuredVersion
            : 0;
        var agentId = agent switch
        {
            CodeGenAgent.RoleExtraction => Value("RoleExtractionAgentId", "AGENT_ID_ROLE_EXTRACTION"),
            CodeGenAgent.LowCode => Value("LowCodeAgentId", "AGENT_ID_SCENARIO_LOWCODE"),
            _ => Value("ChatAgentId", "AGENT_ID_CHAT")
        };

        var requiredValues = new[]
        {
            (Name: "BASE_URL", Value: baseUrl),
            (Name: "API_KEY", Value: apiKey),
            (Name: AgentEnvironmentName(agent), Value: agentId)
        };

        var missing = requiredValues
            .Where(item => IsUnset(item.Value))
            .Select(item => item.Name)
            .ToArray();

        if (missing.Length > 0)
        {
            // A fresh or partially configured local checkout remains usable without sending a
            // request to an invalid endpoint. Real configuration takes over only once complete.
            return "Hello I am your assistent";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
        {
            Content = JsonContent.Create(new
            {
                agent_id = agentId,
                agent_version = agentVersion,
                inputs = messages.Select(message => new { role = message.Role, content = message.Content })
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        var rawJson = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return ParseContent(rawJson);
    }

    private string Value(string configurationName, string environmentName) =>
        _configuration[$"CodeGen:{configurationName}"] ??
        Environment.GetEnvironmentVariable(environmentName) ??
        string.Empty;

    // The tracked development template intentionally contains human-readable placeholders. Treat
    // them exactly like absent values so a fresh clone still uses the local dummy chat response.
    private static bool IsUnset(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Contains("your-", StringComparison.OrdinalIgnoreCase);

    private static string AgentEnvironmentName(CodeGenAgent agent) => agent switch
    {
        CodeGenAgent.RoleExtraction => "AGENT_ID_ROLE_EXTRACTION",
        CodeGenAgent.LowCode => "AGENT_ID_SCENARIO_LOWCODE",
        _ => "AGENT_ID_CHAT"
    };

    private static string ParseContent(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String)
        {
            return result.GetString() ?? string.Empty;
        }

        // The Mistral Conversations API returns agent messages under outputs rather than the
        // chat-completions choices collection. Support both shapes so the same client can use
        // either endpoint.
        if (root.TryGetProperty("outputs", out var outputs) &&
            outputs.ValueKind == JsonValueKind.Array)
        {
            foreach (var output in outputs.EnumerateArray())
            {
                if (output.ValueKind == JsonValueKind.Object &&
                    output.TryGetProperty("content", out var outputContent) &&
                    outputContent.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(outputContent.GetString()))
                {
                    return outputContent.GetString()!;
                }
            }
        }

        // Preserve an unfamiliar response rather than failing the chat turn. This makes new agent
        // response shapes visible in Studio and keeps the raw payload available for inspection.
        return rawJson;
    }
}
#endif
