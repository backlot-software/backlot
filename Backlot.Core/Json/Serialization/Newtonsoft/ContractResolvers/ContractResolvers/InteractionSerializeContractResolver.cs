using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ValueProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ContractResolvers;

public class InteractionSerializeContractResolver : BaseSerializeResolver
{
    internal InteractionSerializeContractResolver(Func<Type, IList<JsonProperty>> getMetaData) 
        : base(getMetaData)
    {
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);
        
        // Adds metadata to roles
       
        if (typeof(IRole).IsAssignableFrom(type)) // make sure we handle Persistable roles.
        {
            foreach (var itm in GetMetaData(type))
            {
                props.Add(itm);
            }
        }
        
        if ((typeof(IPermission)).IsAssignableFrom(type))
        {
            var permissionProp = props.FirstOrDefault(p => p.PropertyName == Meta.__Permission);
            if (permissionProp != null)
            {
                // for interaction the permission property is always part of the serialization.
                permissionProp.Ignored = false;
                DefinePermissionProp(permissionProp);
            }
        }
        
        return props.Where(p => !p.Ignored).ToList();
    }
    
    /// <summary>
    /// The permission property is defined with its own value provider.
    /// </summary>
    /// <param name="permissionProp"></param>
    private void DefinePermissionProp(JsonProperty permissionProp)
    {
        // PropertyName = Meta.__Permission,
        // UnderlyingName = Meta.__Permission,

        permissionProp.PropertyType =
            typeof(object); // is of type dictionary<string, object> while serializing but of type string while deserializing..

        permissionProp.ValueProvider = new PermissionsValueProvider();
        permissionProp.AttributeProvider = NoAttributeProvider.Instance;
        permissionProp.Readable = true;
        permissionProp.Writable = false;

        permissionProp.ItemIsReference = false;
        permissionProp.TypeNameHandling = TypeNameHandling.None;
        permissionProp.NullValueHandling = NullValueHandling.Ignore;
    }
    
    /// <summary>
    /// During serialization calculated properties always are part of the serialization.
    /// - They are either persisted for reference
    /// - Or they are displayed and part of the "view"models.
    /// - If you like to exclude these defaults, override this method.
    /// </summary>
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
            property.ShouldSerialize = instance =>
            {
                if (property.ValueProvider == null) return true;
                var calcValue = property.ValueProvider.GetValue(instance);
                return calcValue != null;
            };
        }

        #endregion

        return property;
    }
}