namespace Backlot.Studio.Core.Models.Request;

/// <summary>
/// The Backlot.Authentication.Bearer "RefreshToken" role, used by the Refresh and Revoke scenarios.
/// </summary>
public class RefreshTokenRequest : IRequestBody
{
    public string? RefreshToken { get; set; }
}
