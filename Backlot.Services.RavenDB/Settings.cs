using System.Diagnostics.CodeAnalysis;
using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Services.RavenDb;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Settings
{
    [Configurable]
    public string ServerUrl { get; set; }
    [Configurable]
    public string DatabaseName { get; set; } 
    
    /// <summary>
    /// How to create a base64 char set of your pfx
    /// ...
    /// byte[] data = File.ReadAllBytes(@"filename.pfx");
    /// var base64 = Convert.ToBase64String(data);
    /// base64.Dump();
    /// var bdata = Convert.FromBase64String(base64);
    /// var cert = new X509Certificate2(bdata);
    /// ...
    /// </summary>
    [Configurable]
    public string X509Certificate2 { get; set; }
}