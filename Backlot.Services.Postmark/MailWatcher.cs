using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stubble.Core.Builders;
using YamlDotNet.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global : done by factory mechanism.

namespace Backlot.Services.Postmark;

/// <summary>
/// The MailWatcher is a watcher that sends an email to the receiver defined in the template.
/// Templates are build using yaml and are using mustache to merge the viewmodel (the scenario) with the template.
/// Sending is done at the ".After" event of the scenario.
/// </summary>
/// <typeparam name="TScenario"></typeparam>
public class MailWatcher<TScenario> : ISceneWatcher<TScenario>
    where TScenario : IScenario
{
    private readonly ILogger<MailWatcher<TScenario>> _logger;
    private readonly IFileSystem _fileSystem;
    
    [Configurable] public string Events { get; set; }
    private string[] EventItms => Events == null ? Enumerable.Empty<string>().ToArray() : Events.Split(",");
    
    [Configurable] public string RequestUri { get; set; }
    [Configurable] public string ServerToken { get; set; }

    [Configurable] public string From { get; set; }

    [Configurable] public string FromName { get; set; }

    [Configurable] public string ReplyTo { get; set; }
    
    
    public MailWatcher(ILogger<MailWatcher<TScenario>> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public void Watch(TScenario scenario)
    {
        scenario.Fired += async (sender, @event) =>
        {
            if(string.IsNullOrWhiteSpace(RequestUri) || string.IsNullOrWhiteSpace(ServerToken) || string.IsNullOrWhiteSpace(From)) return;
            
            if (EventItms.Any(e => e.Equals(@event.EventName, StringComparison.InvariantCultureIgnoreCase)))
            {
                if(sender is TScenario scene) // the scene is the viewmodel
                {
                    var stubble = new StubbleBuilder()
                        .Configure(settings => settings.AddJsonNet())
                        .Build();
                    
                    var filecontents = await _fileSystem.GetFileContentAsync($"{scene.Reference.Name}.mustach.yml");
                    var jsonvm = JObject.FromObject(scene, Strategy.SerializeForInteraction); //json serialized so we are sure custom fields are accessable as well.
                      var renderedContent = await stubble.RenderAsync(filecontents, jsonvm); 
                      var yamlDeserializer = new DeserializerBuilder().Build();
                      var expYml = yamlDeserializer.Deserialize<System.Dynamic.ExpandoObject>(renderedContent);
                      dynamic yml = expYml;

                    if (string.IsNullOrWhiteSpace(yml.To)) return;
                      
                    var contents = new EmailMessage
                    {
                        Bcc = "", 
                        Cc = "", 
                        From = From,
                        To = yml.To,
                        ReplyTo = ReplyTo,
                        Subject = yml.Subject,
                        TextBody = yml.Text,
                        HtmlBody = yml.Html,
                        MessageStream = "outbound"
                    };
                    
                    if(((IDictionary<string, object>)expYml).ContainsKey("Bcc"))
                    {
                        contents.Bcc = yml.Bcc;
                    }
                    
                    if(((IDictionary<string, object>)expYml).ContainsKey("Cc"))
                    {
                        contents.Cc = yml.Cc;
                    }
        
                    try
                    {
                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Post,
                            RequestUri = new Uri(RequestUri),
                            Headers =
                            {
                                { "user-agent", "versla-client" },
                                { "accept", "application/json" },
                                { "x-postmark-server-token", ServerToken },
                            },
                            Content = new StringContent(JsonConvert.SerializeObject(contents))
                            {
                                Headers =
                                {
                                    ContentType = new MediaTypeHeaderValue("application/json")
                                }
                            }
                        };

                    
                        var client = new HttpClient();
                        using var response = await client.SendAsync(request);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError("An unsuccesfull {@Response} was received while sending mail for scenario '{@Reference}' with {Role}. Within '{Clss}.{Fn}'",
                                response.Content.ReadAsStringAsync().Result,
                                scene.Reference,
                                scene.Role.GetFriendlyReference(),
                                nameof(MailWatcher<TScenario>),
                                nameof(Watch)
                            );
                        }
                    }
                    catch (ArgumentNullException e)
                    {
                        _logger.LogError(e, "An ArgumentNullException occured while sending mail for scenario '{@Reference}' with {Role}. Within '{Clss}.{Fn}'",
                            scene.Reference,
                            scene.Role.GetFriendlyReference(),
                            nameof(MailWatcher<TScenario>),
                            nameof(Watch)
                        );
                    }
                }
            }
        };
    }

    void IWatcher.Watch(IScenario scenario)
    {
        Watch((TScenario)scenario);
    }
}