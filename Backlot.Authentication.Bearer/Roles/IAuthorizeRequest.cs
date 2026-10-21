using Backlot.Core;

namespace Backlot.Authentication.Bearer.Roles;

/// <summary>
/// Start of the authorization code flow (PKCE, S256 only).
/// </summary>
public interface IAuthorizeRequest : IRole
{
    /// <summary>Where the browser is sent back to with ?code=..&amp;state=.. (must be in AllowedRedirectUris).</summary>
    public string RedirectUri { get; set; }

    /// <summary>Opaque client value, returned unchanged.</summary>
    public string State { get; set; }

    /// <summary>base64url(SHA256(code_verifier))</summary>
    public string CodeChallenge { get; set; }
}
