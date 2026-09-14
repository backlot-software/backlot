using System.Text.Json;
using Backlot.Studio.Areas.Studio.Pages.ViewModels;
using Backlot.Studio.Core;
using Backlot.Studio.Core.Models;
using Backlot.Studio.Core.Models.Request;
using Backlot.Studio.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace Backlot.Studio.Areas.Studio.Pages.Development;

// Development — a chat window for designing an application out loud. The operator describes what
// they want; Backlot.Services.AI answers with the next question and keeps a running list of the
// roles and scenarios the conversation implies, shown as two panes beside the chat.
//
// The Mistral call itself happens server-side inside the API (scenario "create" on role
// IConversation), so the API key never leaves the host — the same boundary every other Studio page
// works within. Studio only proxies the turn and remembers the transcript.
public class IndexModel : AuthenticatedPageModel
{
    private const string RoleName = "conversation";
    private const string ScenarioName = "create";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBacklotApiClient _api;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IBacklotApiClient api, ILogger<IndexModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>Conversation rendered server-side, so a Turbo return visit shows it already in progress.</summary>
    public DevelopmentConversation Chat { get; private set; } = new();

    public void OnGet()
    {
        SetUserContext();

        // No API call on GET: the opening question is Studio's own, not the model's.
        var state = LoadState();
        if (state == null)
        {
            state = DevelopmentConversation.Opening();
            SaveState(state);
        }

        Chat = state;
    }

    public class SendInput
    {
        public string Message { get; set; } = string.Empty;
    }

    [BindProperty]
    public SendInput Input { get; set; } = new();

    // OnPostSendAsync — invoked via fetch() from the page. Plays one turn and returns the assistant's
    // reply plus both updated name lists as JSON. Session is only written after the turn succeeds, so
    // a failed call never leaves a half-finished exchange behind.
    public async Task<IActionResult> OnPostSendAsync(CancellationToken ct)
    {
        var message = Input.Message?.Trim() ?? string.Empty;
        if (message.Length == 0)
        {
            return new JsonResult(new { error = "Type a message first." })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var state = LoadState() ?? DevelopmentConversation.Opening();

        try
        {
            var envelope = await _api.Play<Conversation, Conversation>(RoleName, ScenarioName, new Conversation
            {
                Message = message,
                Roles = state.Roles.ToArray(),
                Scenarios = state.Scenarios.ToArray()
            }, ct);

            var reply = envelope?.Body;
            if (reply == null)
            {
                return new JsonResult(new { error = "The AI service returned an empty answer." })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            state.Turns.Add(new ChatTurn { FromUser = true, Text = message });
            state.Turns.Add(new ChatTurn { FromUser = false, Text = reply.Message });
            state.Roles = [.. reply.Roles ?? []];
            state.Scenarios = [.. reply.Scenarios ?? []];
            SaveState(state);

            return new JsonResult(new
            {
                message = reply.Message,
                roles = state.Roles,
                scenarios = state.Scenarios
            });
        }
        catch (UnauthorizedAccessException)
        {
            // Credentials expired/invalid — send the operator back through login rather than
            // silently failing, same as the Client page.
            return new JsonResult(new { unauthorized = true, error = "Unauthorized — your session may have expired. Please sign in again." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }
        catch (BacklotApiException ex)
        {
            // The API rejected the turn — a missing Mistral key and a malformed model reply both
            // arrive here as a 400 whose envelope Body is the operator-readable reason.
            _logger.LogWarning(ex, "The create scenario rejected a Development turn");
            return new JsonResult(new { error = DescribeApiFailure(ex) });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Development turn failed to reach the Backlot API");
            return new JsonResult(new { error = "Request failed. Check that the Backlot API is reachable." })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }

    // Backlot wraps an error message in the standard envelope ({ Body, Status, ... }). Unwrap it so
    // the chat shows "…is not configured: set …ApiKey" rather than the raw JSON; fall back to the
    // exception's own text when the body is not an envelope.
    private static string DescribeApiFailure(BacklotApiException ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<ApiEnvelope<string>>(ex.ResponseBody, JsonOptions);
                if (!string.IsNullOrWhiteSpace(envelope?.Body))
                {
                    return envelope.Body;
                }
            }
            catch (JsonException)
            {
                // Not an envelope — fall through to the generic message below.
            }
        }

        return "The AI service could not answer this message.";
    }

    private DevelopmentConversation? LoadState()
    {
        var json = HttpContext.Session.GetString(DevelopmentConversation.SessionKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<DevelopmentConversation>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Stored by an older shape — start over rather than failing the page.
            return null;
        }
    }

    private void SaveState(DevelopmentConversation state) =>
        HttpContext.Session.SetString(DevelopmentConversation.SessionKey, JsonSerializer.Serialize(state, JsonOptions));
}
