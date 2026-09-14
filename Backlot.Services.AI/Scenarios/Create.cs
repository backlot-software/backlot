using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.AI.Mistral;
using Backlot.Services.AI.Roles;

namespace Backlot.Services.AI.Scenarios;

/// <summary>
/// One turn of a design conversation: take what the developer said plus the roles and scenarios
/// agreed so far, ask the model, and hand back the same shape with the reply and the extended
/// lists.
/// </summary>
[Scenario(typeof(Create), access: [Access.Everyone])]
public class Create : Scenario<Create, IConversation, IConversation>
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfigurationManager _configurationManager;
    
    public Create(IConversation role, IHttpClientFactory clients, IConfigurationManager configurationManager)
        : base(role)
    {
        _clients = clients;
        _configurationManager = configurationManager;
    }

    // A chat turn is not state - nothing here belongs in the database.
    protected override bool PersistAndRelate => false;

    public override bool Validate()
    {
        if (!string.IsNullOrWhiteSpace(Role.Message))
        {
            return base.Validate();
        }

        ValidationResults.Add(new ValidationResult("A message is required.", [nameof(IConversation.Message)]));
        return false;
    }

    protected override async Task<IConversation> ExecAsync()
    {
        var roles = Normalize(Role.Roles);
        var scenarios = Normalize(Role.Scenarios);

        var client = new MistralChatClient(
            _clients,
            _configurationManager.Get<Settings>(s => s.ApiKey),
            _configurationManager.Get<Settings>(s => s.Endpoint),
            _configurationManager.Get<Settings>(s => s.Model));

        var reply = await client.CompleteAsync(
            MistralChatClient.SystemPrompt,
            MistralChatClient.BuildUserPrompt(Role.Message, roles, scenarios),
            CancellationToken.None);

        // Union rather than replace: the model is told to return the complete lists, but a bad turn
        // must never be able to drop names the conversation already agreed on.
        Role.Message = reply.Message;
        Role.Roles = Merge(roles, reply.Roles);
        Role.Scenarios = Merge(scenarios, reply.Scenarios);

        return Role;
    }

    private static string[] Normalize(string[] values) =>
        (values ?? [])
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static string[] Merge(IEnumerable<string> existing, IEnumerable<string> added) =>
        existing
            .Concat(Normalize((added ?? []).ToArray()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
