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
    
    /// <summary>
    /// Shortest substring ("contains") search the role index supports. Default: 3.
    /// Search terms shorter than this fall back to a prefix (starts-with) match instead
    /// of a true contains. Lowering it to 2 enables 2-character substring search but
    /// significantly inflates the index and slows short searches on large databases;
    /// raising it shrinks the index further at the cost of 3-character contains-search.
    /// Changing this value requires the Roles/BySkillAndReadPermission index to rebuild.
    /// </summary>
    [Configurable]
    public string NGramMinGram { get; set; }

    /// <summary>
    /// Longest indexed substring fragment for "contains" searches. Default: 6.
    /// Longer search terms still work: they are cut into fragments of this length behind
    /// the scenes. Raising it slightly speeds up long search terms but grows the index;
    /// lowering it shrinks the index but makes long searches intersect more fragments.
    /// Must be greater than or equal to NGramMinGram.
    /// Changing this value requires the Roles/BySkillAndReadPermission index to rebuild.
    /// </summary>
    [Configurable]
    public string NGramMaxGram { get; set; }
    
}