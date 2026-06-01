using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Experimental.Authentication.Auth0;

public class Settings
{
    [Configurable]
    public string Auth0Domain { get; set; }
    
    [Configurable]
    public string Auth0Audience { get; set; }

    /// <summary>
    /// The claim name used to define the unique username.
    /// </summary>
    [Configurable]
    public string UserNameClaim { get; set; } = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
}