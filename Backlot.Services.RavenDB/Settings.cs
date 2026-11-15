using System.Diagnostics.CodeAnalysis;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;

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

    /// <summary>
    /// Maximum number of characters of a string value that is indexed. Default: 1024.
    /// Longer values are truncated before indexing, so "contains" only finds text within the
    /// first characters and "equals" compares the truncated values. Large texts (e.g. markdown)
    /// otherwise overflow the ngram analyzer buffer of the Corax search engine and fail indexing.
    /// Changing this value requires the Roles/BySkillAndReadPermission index to rebuild.
    /// </summary>
    [Configurable]
    public string IndexMaxFieldLength { get; set; }

    /// <summary>
    /// Comma separated list of document fields that are never indexed, e.g. "Content,Body".
    /// Matched case sensitive against the json field name. Criteria on an excluded field match nothing,
    /// and ordering on it has no effect.
    /// Changing this value requires the Roles/BySkillAndReadPermission index to rebuild.
    /// </summary>
    [Configurable]
    public string IndexExcludedFields { get; set; }

}

/// <summary>
/// Get shared settings.
/// </summary>
internal static class IndexConfig
{
    internal static int NGramMinGram => Convert.ToInt32(ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.NGramMinGram) ?? "3");
    
    internal static int NGramMaxGram => Convert.ToInt32(ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.NGramMaxGram) ?? "6");

    internal static int IndexMaxFieldLength => Convert.ToInt32(ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.IndexMaxFieldLength) ?? "1024");

    internal static IEnumerable<string> IndexExcludedFields
    {
        get
        {
            var fields = ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.IndexExcludedFields) ?? string.Empty;
            return string.IsNullOrWhiteSpace(fields) ? Enumerable.Empty<string>() : fields.Split(',');
        }
    }
}