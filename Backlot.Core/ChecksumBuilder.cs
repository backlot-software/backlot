using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backlot.Core;

public interface IChecksumBuilder
{
    string BuildHash(IRole role);
}

public class ChecksumBuilder : IChecksumBuilder // inspired by; // https://codereview.stackexchange.com/questions/188522/calculate-fingerprint-for-an-object and changed to the needs of Backlot.
{
    private readonly Func<byte[], byte[]> _computeHash;

    /// <summary>
    /// System properties are always ignored.
    /// </summary>
    private string[] _systemProperties =
    [
        Meta.__Construct,
        Meta.__Skills,
        nameof(IJProxy.JActor),
        nameof(IProxiedRole.Actor),
        nameof(IPersist.LastModified)
    ];
    private readonly IDictionary<string, string[]> _ignoredPropertiesPerSkill;
    

    public ChecksumBuilder(Func<byte[], byte[]> computeHash) //check
    {
        _computeHash = computeHash ?? throw new ArgumentNullException(nameof(computeHash));
        _ignoredPropertiesPerSkill = DefineIgnored();
    }

    private IDictionary<string, string[]> DefineIgnored()
    {
        var dic = new Dictionary<string, string[]>();
        
        var roles = Loader.AllRoles;

        foreach (var role in roles)
        {
            var ignoredProperties = role.GetProperties().Where(prop =>
            {
                if (prop.Name == Meta.__Permission) return false; // from all ignored properties only permissions need to be part of the checksum, because it need to be updated when changed.
                var att = prop.GetCustomAttributes(false).ToList();
                return att.OfType<CalculatedAttribute>().Any() || att.OfType<JsonIgnoreAttribute>().Any();
            }).Select(p => p.Name).ToArray();
            
            if(ignoredProperties.Any()) dic.Add(role.GetRoleName(), ignoredProperties);
        }

        return dic;
    }
    
    public string BuildHash(IRole role)
    {
        // checksums are calculated on a flat representation of role and actor using the ForPersistance strategy, because checksums are based on the "persited" state.
        var json = JObject.FromObject(role, Strategy.SerializeForPersistance);
        var fingerprints =  new SortedDictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);
        var skills = role.Skills(); // when one of the skills has ignored properties ignore them for the checksum.
        var ignoredPropertiesForChecksum = _ignoredPropertiesPerSkill
            .Where(itm => skills.Any(s => s == itm.Key))
            .SelectMany(itm => itm.Value)
            .Concat(_systemProperties)
            .Distinct()
            .ToList();
        
        foreach (var prop in json?.Properties().Where(p => ignoredPropertiesForChecksum.All(pl => pl != p.Name))!)
        {
            if (!fingerprints.ContainsKey(prop.Name))
            {
                fingerprints[prop.Name] = () => prop.Value.ToString();
            }
        }

        if (role is IPermission po)
            fingerprints[Meta.__Permission] = () => role.Permission().ToString();

        using var m = new MemoryStream();
        using (var writer = new BinaryWriter(m))
        {
            foreach (var itm in fingerprints)
            {
                writer.Write(JsonConvert.SerializeObject(itm.Value()));
            }
        }
        
        return ToHexString(_computeHash(m.ToArray()));
    }

    private static string ToHexString(byte[] source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return string.Join("", source.Select(ch => ch.ToString("X2")));
    }
}
