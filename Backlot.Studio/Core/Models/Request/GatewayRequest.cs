namespace Backlot.Studio.Core.Models.Request;

/// <summary>
/// The Backlot.Authentication.Bearer "Gateway" role, used by the Authorize, Callback and Access scenarios.
/// Each scenario only needs some of the properties.
/// </summary>
public class GatewayRequest : IRequestBody
{
    public string? RedirectUrl { get; set; }
    public string? State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? Code { get; set; }
    public string? CodeVerifier { get; set; }
}
