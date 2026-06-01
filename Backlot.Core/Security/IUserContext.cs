using System.Collections.Generic;

namespace Backlot.Core.Security;

/// <summary>
/// The central place where authentication and authorization starts.
/// ----
/// No matter what middleware or service is used, this interface should be implemented.
/// Per middleware or service different contracts and settings can be used,
/// but the IUserContext is used to spread the information about the user and their claims,
/// throughout the application.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// The authentication scheme used for the token (examples can be "Bearer", "Basic" or "Digest")
    /// </summary>
    public string AuthScheme { get; }
    
    /// <summary>
    /// At token set the initialization of the whole context starts.
    /// </summary>
    public string Token { set; }
    public string UserName { get; }
    public IDictionary<string, string> Claims { get; }
    public string[] Groups { get; }
    public bool IsInGroup(string groupname);
    public bool IsAuthenticated { get; }
}