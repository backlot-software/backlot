using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ContractResolvers;

public abstract class BaseSerializeResolver : DefaultContractResolver
{
    protected Func<Type, IList<JsonProperty>> GetMetaData;
    
    public BaseSerializeResolver(Func<Type, IList<JsonProperty>> getMetaData)
    {
        GetMetaData = getMetaData;
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
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization); 
        return CreatePropertyImpl(property, member, memberSerialization);
    }

    protected abstract JsonProperty CreatePropertyImpl(JsonProperty property, MemberInfo member,
        MemberSerialization memberSerialization);
}