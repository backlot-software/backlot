using System.Net.Http.Json;
using System.Text.Json;
using Backlot.Demo.Web.Roles;

namespace Backlot.Demo.Web.Services;

public class CodeGenAgentService(HttpClient httpClient, IConfiguration configuration) : ICodeGenAgentService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;

    public async Task<string> SendAsync(IReadOnlyCollection<IChatMessage> messages, CodeGenAgent agent)
    {
        var baseUrl = Value("BaseUrl", "BASE_URL");
        var apiKey = Value("ApiKey", "API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey)) apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? string.Empty;

        var agentVersion = int.TryParse(_configuration["CodeGen:AgentVersion"], out var configuredVersion) ? configuredVersion : 0;
        var agentId = agent switch
        {
            CodeGenAgent.RoleExtraction => Value("RoleExtractionAgentId", "AGENT_ID_ROLE_EXTRACTION"),
            CodeGenAgent.LowCode => Value("LowCodeAgentId", "AGENT_ID_SCENARIO_LOWCODE"),
            _ => Value("ChatAgentId", "AGENT_ID_CHAT")
        };

        if (new[] { baseUrl, apiKey, agentId }.Any(IsUnset)) return "Hello I am your assistent";

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
        _configuration[$"CodeGen:{configurationName}"] ?? Environment.GetEnvironmentVariable(environmentName) ?? string.Empty;

    private static bool IsUnset(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Contains("your-", StringComparison.OrdinalIgnoreCase);

    private static string ParseContent(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 && choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String)
            return result.GetString() ?? string.Empty;

        if (root.TryGetProperty("outputs", out var outputs) && outputs.ValueKind == JsonValueKind.Array)
        {
            foreach (var output in outputs.EnumerateArray())
            {
                if (output.ValueKind == JsonValueKind.Object && output.TryGetProperty("content", out var outputContent) &&
                    outputContent.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(outputContent.GetString()))
                    return outputContent.GetString()!;
            }
        }

        return rawJson;
    }
}
