using System.Collections.Generic;

namespace Backlot.Core.Services;

/// <summary>
/// Used for low traffic none load balanced websites and demo purposes
/// </summary>
internal static class MemoryStore
{
    /// <summary>
    /// Key: Uid
    /// Value: Checksum
    /// </summary>
    /// <returns></returns>
    internal static IDictionary<string, string> Checksums;
    
    internal static IDictionary<string, string> Store;
    internal static IList<Relation> Relations;

    internal static void Initialize()
    {
        Store ??= new Dictionary<string, string>();
        Relations ??= new List<Relation>();
        Checksums ??= new Dictionary<string, string>();
    }
}