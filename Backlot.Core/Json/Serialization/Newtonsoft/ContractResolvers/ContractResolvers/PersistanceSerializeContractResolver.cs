using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ContractResolvers;

/// <summary>
/// Includes meta data but ignores calculated fields for reference.
/// </summary>
public class PersistanceSerializeContractResolver : BaseSerializeResolver
{
    public PersistanceSerializeContractResolver(Func<Type, IList<JsonProperty>> getMetaData) : base(getMetaData)
    {
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);

        if (typeof(IRole).IsAssignableFrom(type)) // make sure we handle Persistable roles.
        {
            foreach (var itm in GetMetaData(type))
            {
                props.Add(itm);
            }
        }

        if (typeof(IPersist).IsAssignableFrom(type)) // make sure we handle Persistable roles.
        {
            // ignore lastmodified date.
            var lastmodified = props.FirstOrDefault(p => p.PropertyName == nameof(IPersist.LastModified));
            if (lastmodified != null)
            {
                lastmodified.Ignored = true;
            }

            if (typeof(IPermission).IsAssignableFrom(type))
            {
                var permissionProp = props.FirstOrDefault(p => p.PropertyName == Meta.__Permission);
                if (permissionProp != null)
                {
                    // for persistance this is always ignored, because we do set this differently per database.
                    permissionProp.Ignored = true;
                }
            }
        }

        return props.Where(p => !p.Ignored).ToList();
    }

    /// <summary>
    /// Ignore calculated properties.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="member"></param>
    /// <param name="memberSerialization"></param>
    /// <returns></returns>
    protected override JsonProperty CreatePropertyImpl(JsonProperty property, MemberInfo member, MemberSerialization memberSerialization)
    {
        #region Calculated

        var hasCalculatedattribute = member.DeclaringType?.GetInterfaces().Any(i =>
        {
            var any = i.GetProperties().FirstOrDefault(p => p.Name == member.Name)?.GetCustomAttributes()
                .Any(att => att is CalculatedAttribute);

            return any ?? false;
        });

        if (hasCalculatedattribute.HasValue && hasCalculatedattribute.Value)
        {
            property.Ignored = true; // Ignore calculated properties.
        }

        #endregion

        return property;
    }
}