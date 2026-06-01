using System;
using System.Linq;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core.Security;

namespace Backlot.Authentication.BuiltIn;

/// <summary>
/// The Implementation of the IUserContext when using a tokenservice.
/// </summary>
public class BuiltInUserContext : TokenHandlerBase, IUserContext
{
    public BuiltInUserContext(JwtTokenService tokenService) : base(tokenService, null)
    {
    }

    public string AuthScheme => "Bearer";

    /// <summary>
    /// Needs to be set by the authentication middleware
    /// </summary>
    public string Token
    {
        set => TokenAndPrincipal = TokenService.GetTokenAndPrincipals(value);
    }
    
    /// <summary>
    /// The groups this user is in.
    /// </summary>
    public string[] Groups => Claims[TokenService.ClaimNames.Groups].Split(",");
    
    public bool IsInGroup(string groupname)
    {
        return Groups
            .Any(r => r.Equals(groupname, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// Is this user authenticated?
    /// </summary>
    public bool IsAuthenticated => IsValid;
}