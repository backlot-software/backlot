using Backlot.Core;

namespace Backlot.Services.AI.Roles;

/// <summary>
/// One turn of a design conversation. It is a plain request role - not IPersist, not IUid - so
/// nothing about a chat turn is written to the database and the endpoint is POST-only
/// (<c>POST /api/role/conversation/create</c>). Same shape as <c>ISimpleQuery</c>.
///
/// The role carries no transcript by design: a turn is (what the developer just said) plus
/// (everything agreed so far, as names). The Create scenario returns the same three properties
/// with Message replaced by the assistant's reply and the two lists extended.
/// </summary>
public interface IConversation : IRole
{
    /// <summary>Inbound: what the developer typed. Outbound: the assistant's reply or question.</summary>
    string Message { get; set; }

    /// <summary>Role names agreed on so far, e.g. "IProduct". Names only - no source, no schema.</summary>
    string[] Roles { get; set; }

    /// <summary>Scenario names agreed on so far, e.g. "Checkout". Names only.</summary>
    string[] Scenarios { get; set; }
}
