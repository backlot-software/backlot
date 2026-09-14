using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Services.AI;

/// <summary>
/// Configuration for the AI service, resolved through <c>IConfigurationManager</c> which keys on
/// the full type name. In a <c>{Environment}.jsonsettings.json</c> that is:
/// <code>
/// Backlot > Services > AI > Settings > { ApiKey, Endpoint, Model }
/// </code>
/// Only string/int/decimal/float/bool are supported by <c>BaseSettingsManager</c>, so every value
/// here is a string.
/// </summary>
public class Settings
{
    /// <summary>
    /// Mistral API key. Leave this empty in a committed settings file: the key is then read from
    /// the MISTRAL_API_KEY environment variable instead (see <c>MistralChatClient</c>).
    /// </summary>
    [Configurable]
    public string ApiKey { get; set; }

    /// <summary>Chat-completions endpoint. Empty falls back to <c>Defaults.Endpoint</c>.</summary>
    [Configurable]
    public string Endpoint { get; set; }

    /// <summary>Model name. Empty falls back to <c>Defaults.Model</c>.</summary>
    [Configurable]
    public string Model { get; set; }
}

/// <summary>
/// Values used whenever the corresponding <see cref="Settings"/> entry is absent or blank, so the
/// service works with nothing configured but the API key.
/// </summary>
public static class Defaults
{
    public const string Endpoint = "https://api.mistral.ai/v1/chat/completions";
    public const string Model = "mistral-large-latest";

    /// <summary>Environment variable consulted when <see cref="Settings.ApiKey"/> is blank.</summary>
    public const string ApiKeyEnvironmentVariable = "MISTRAL_API_KEY";
}
