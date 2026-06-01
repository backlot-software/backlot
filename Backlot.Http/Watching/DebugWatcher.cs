using System.Net.Http.Headers;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global : properties initialized by factory.

namespace Backlot.Http.Watching;

/// <summary>
/// A watcher that logs events to a configured webhook endpoint.
/// </summary>
public class DebugWatcher : IBingeWatcher
{
    private readonly ILogger<DebugWatcher> _logger;

    #region Settings

    [Configurable] public string WebHookEndpoint { get; set; }
    [Configurable] public string Events { get; set; }
    [Configurable] public string Scenarios { get; set; }

    private string[] ScenarioItms => Scenarios.Split(",");
    private string[] EventItms => Events.Split(",");

    #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. set within factory mechanisme.
    public DebugWatcher(ILogger<DebugWatcher> logger)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _logger = logger;
    }

    public void Watch(IScenario scenario)
    {
        _logger.LogDebug("Initialize watching scenario '{@Reference}' with {Role} for the events {Events}. Within '{Clss}.{Fn}'",
            scenario.Reference,
            scenario.Role.GetFriendlyReference(),
            Events,
            nameof(DebugWatcher),
            nameof(Watch)
        );
        
        if (!string.IsNullOrEmpty(WebHookEndpoint))
        {
            if (ScenarioItms.Any(itm => itm.Equals(scenario.Reference.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                scenario.Fired += async (_, args) =>
                {
                    if (EventItms.Any(itm =>
                            itm.Equals(args.EventName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        
                        _logger.LogDebug("Monitoring scenario '{@Reference}' with {Role} during {Event}. Within '{Clss}.{Fn}'",
                            scenario.Reference,
                            scenario.Role.GetFriendlyReference(),
                            args.EventName,
                            nameof(DebugWatcher),
                            nameof(Watch)
                        );

                        var client = new HttpClient();
                        var serializer = Strategy.SerializeForInteraction;
                        var url = WebHookEndpoint;
                        var content = "{ \"Eventname\": \"" + args.EventName + "\","
                                      + "\"Reference\":" + Json.ToJson(scenario.Reference, serializer) + ","
                                      + "\"Scenario\":" + Json.ToJson(scenario, serializer) + ","
                                      + "\"Role\":" + Json.ToJson(scenario.Role, serializer) + ","
                                      + "\"Result\": " + Json.ToJson(scenario.ResultValue, serializer) +
                                      "" //no , for the last item
                                      + "}";

                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Post,
                            RequestUri = new Uri(url),
                            Headers =
                            {
                                { "user-agent", "versla-client" },
                                { "accept", "application/json" }
                                //todo; optional configurable security token??
                            },
                            Content = new StringContent(content)
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                            }
                        };

                        using (var response = await client.SendAsync(request))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.LogError("An unsuccesfull {@Response} was received while sending debug information for scenario '{@Reference}' with {Role}. Within '{Clss}.{Fn}'",
                                    response.Content.ReadAsStringAsync().Result,
                                    scenario.Reference,
                                    scenario.Role.GetFriendlyReference(),
                                    nameof(DebugWatcher),
                                    nameof(Watch)
                                );
                            }
                        }
                    }
                };
            }
        }
    }
}