using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

/// <summary>
/// Token request:
/// - GrantType "authorization_code": Code, CodeVerifier and RedirectUri are required.
/// - GrantType "refresh_token": RefreshToken is required.
/// </summary>
public interface ITokenRequest : IRole
{
    public string GrantType { get; set; }
    public string Code { get; set; }
    public string CodeVerifier { get; set; }
    public string RedirectUri { get; set; }
    public string RefreshToken { get; set; }
}
