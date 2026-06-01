using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ContractResolvers;

/// <summary>
/// All metadata and or caluclated fields are ignored within this resolver.
/// </summary>
public class NoMetaDataContractResolver : BaseSerializeResolver
{
    public NoMetaDataContractResolver() : base(_ => { return new List<JsonProperty>();})
    {
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);

        // remove existing metadata ->
        
        if ((typeof(IPermission)).IsAssignableFrom(type))
        {
            var permissionProp = props.FirstOrDefault(p => p.PropertyName == Meta.__Permission);
            if (permissionProp != null)
            {
                permissionProp.Ignored = true;
            }
        }
        
        return props.Where(p => !p.Ignored).ToList();
    }

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