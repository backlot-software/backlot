using System.Diagnostics.CodeAnalysis;
using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Authentication.BuiltIn;

/// <summary>
/// General Backlot Server settings which can be used by more scenarios.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Settings
{
    
#pragma warning disable CS8618 ///properties don't need to be declared in the constructor when they are configurable

    /// <summary>
    /// The root url of your backlot azure functions instance
    /// </summary>
    [Configurable]
    public string RootUrl { get; set; }
    
        
    /// <summary>
    /// Default url for the admin environment, can f.e. used for unique urls in mails for login.
    /// </summary>
    [Configurable]
    public string UiUrl { get; set; }
    
    /// <summary>
    /// Define if authentication scenarios do manage Token Revocation
    /// </summary>
    [Configurable]
    public string ManageTokenRevocation { get; set; }
    
#pragma warning restore CS8618
}
