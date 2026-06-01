using System;
using System.Collections.Generic;
using Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers;
using Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ContractResolvers;
using Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ValueProviders;
using Backlot.Core.Json.Serialization.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft;

/// <summary>
/// Serializing strategy
/// More info; https://3.basecamp.com/3094795/buckets/24365546/todos/7211927195
/// </summary>
public static class Strategy
{
    /// <summary>
    /// Newtonsoft doesn't have a copy method for JsonSerializers.
    /// </summary>
    /// <param name="serializer"></param>
    /// <returns></returns>
    internal static JsonSerializer CreateDeepClone(JsonSerializer serializer)
    {
        var copied = new JsonSerializer
        {
            ContractResolver = serializer.ContractResolver,
            ReferenceLoopHandling = serializer.ReferenceLoopHandling,
            NullValueHandling = serializer.NullValueHandling,
            
            // Copy settings from the original serializer
            Formatting = serializer.Formatting,
            DefaultValueHandling = serializer.DefaultValueHandling,
            ObjectCreationHandling = serializer.ObjectCreationHandling,
            ConstructorHandling = serializer.ConstructorHandling,
            MetadataPropertyHandling = serializer.MetadataPropertyHandling,
            TypeNameHandling = serializer.TypeNameHandling,
            TraceWriter = serializer.TraceWriter,
            EqualityComparer = serializer.EqualityComparer,
        };

        // Copy converters but exclude FlatProxiedRoleConverter
        foreach (var converter in serializer.Converters)
        {
            copied.Converters.Add(converter);
        }
        
        return copied;
    }

    #region Properties

    private static JsonProperty Construct(Type declaringType)
    {
        return new JsonProperty
        {
            DeclaringType = declaringType,
            PropertyName = Meta.__Construct,
            UnderlyingName = Meta.__Construct,
            PropertyType = typeof(string),
            ValueProvider = new ConstructValueProvider(),
            AttributeProvider = NoAttributeProvider.Instance,
            Readable = true,
            Writable = false,
            // Ensure PreserveReferencesHandling and TypeNameHandling do not apply to the synthetic property.
            ItemIsReference = false,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };
    }
    
    private static JsonProperty Skills(Type declaringType)
    {
        return new JsonProperty
        {
            DeclaringType = declaringType,
            PropertyName = Meta.__Skills,
            UnderlyingName = Meta.__Skills,
            PropertyType = typeof(string[]),
            ValueProvider = new SkillsValueProvider(),
            AttributeProvider = NoAttributeProvider.Instance,
            Readable = true,
            Writable = false,
            // Ensure PreserveReferencesHandling and TypeNameHandling do not apply to the synthetic property.
            ItemIsReference = false,
            TypeNameHandling = TypeNameHandling.None,
        };
    }
    
    #endregion
    
    #region Serialize

    public static JsonSerializer SerializeSafe { get; } = CreateSerializeSafe();

    /// <summary>
    /// Serialize objects (or role) as-is without:
    /// - Any calculated fields
    /// - Any meta-data
    /// - Any Actor properties
    /// By using; All available converters for Backlot/backlot
    /// This is the safest, cleanest stragey you can use.
    /// Used by JToken.FromObject and/or .ToJson calls
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateSerializeSafe()
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new NoMetaDataContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });
        
        serializer.Converters.Add(new NumbersConverter());
        serializer.Converters.Add(new SenarioReferenceConverter());
        serializer.Converters.Add(new RoleReferenceConverter());
        serializer.Converters.Add(new RelationConverter());

        return serializer;
    }
    
    public static JsonSerializer SerializeForPersistance { get; } = CreateSerializeForPersistance();
    
    /// <summary>
    /// Used for role and relation persistance
    /// Contains;
    /// Includes meta data:
    /// - __Construct,  __Skills
    /// Does not contains;
    /// - __Permission, because it's set by the repository itself
    /// - LastModifiedData, because it's set by the repository itself
    /// - Calculated properties
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateSerializeForPersistance() // trusted destination
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new PersistanceSerializeContractResolver(type =>
            {
                return new List<JsonProperty>
                {
                    Construct(type),
                    Skills(type),
                };
            }),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });

        serializer.Converters.Add(new FlatProxiedRoleRootConverter()); // todo: includeCalculatedProperties: false));
        
        serializer.Converters.Add(new NumbersConverter());
        serializer.Converters.Add(new SenarioReferenceConverter());
        serializer.Converters.Add(new RoleReferenceConverter());

        return serializer;
    }
    
    public static JsonSerializer SerializeForInteraction { get; } = CreateSerializeForInteraction();
    
    /// <summary>
    /// Serialize for interaction with a user
    /// - Merge the actor into the root object.
    /// - Contains Metadata for reference.
    /// - Contains Calculated fields
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateSerializeForInteraction() // untrusted destination
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new InteractionSerializeContractResolver(type =>
            {
                return new List<JsonProperty>
                {
                    Construct(type),
                    Skills(type)
                };
            }),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore //includeCalculatedPropertiesAndNullValues ? NullValueHandling.Include : NullValueHandling.Ignore
        });
        
        serializer.Converters.Add(new FlatProxiedRoleRootConverter());
        
        serializer.Converters.Add(new NumbersConverter());
        serializer.Converters.Add(new SenarioReferenceConverter());
        serializer.Converters.Add(new RoleReferenceConverter());

        return serializer;
    }
    
    #endregion
    
    #region Deserialize
    
    internal static JsonSerializer DeSerializeSenarioReferenceOnly { get; } = CreateDeSerializeSenarioReferenceOnly();
    
    /// <summary>
    /// Used by ToObjects
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateDeSerializeSenarioReferenceOnly()
    {
        var serializer = JsonSerializer.CreateDefault();
        serializer.Converters.Add(new SenarioReferenceConverter());
        return serializer;
    }
    
    
    internal static JsonSerializer DeSerializeRoleReferenceOnly { get; } = CreateDeSerializeRoleReferenceOnly();
    /// <summary>
    /// Used by ToObjects
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateDeSerializeRoleReferenceOnly()
    {
        var serializer = JsonSerializer.CreateDefault();
        serializer.Converters.Add(new RoleReferenceConverter());
        return serializer;
    }
    
    /// <summary>
    /// Deserialization of JObjects and or strings.
    /// Mainly used by trusted sources like the DB Repositories.
    /// You can assume that the JSON complies with the rules of SerializeForPersistance
    /// </summary>
    public static JsonSerializer DeSerializeFromTrustedSource { get; } = CreateDeSerialize();
    
    
    /// <summary>
    /// Deserialization of JObjects and or strings.
    /// Mainly used by untrusted sources where the origin can be manipulated by humans.
    /// This is the default one to use.
    /// </summary>
    public static JsonSerializer DeSerializeDefault { get; } = CreateDeSerialize();

    /// <summary>
    /// Implementation wise we currently do not make any difference.
    /// But we can change this for the future. To make people aware by having to Deserializers we can change this more easily in the future.
    /// </summary>
    /// <returns></returns>
    private static JsonSerializer CreateDeSerialize()
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new BaseDeSerializeResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include //includeCalculatedPropertiesAndNullValues ? NullValueHandling.Include : NullValueHandling.Ignore
        });
        
        // readonly
        
        // can read&write;
        serializer.Converters.Add(new NumbersConverter());
        serializer.Converters.Add(new SenarioReferenceConverter());
        
        // read only
        serializer.Converters.Add(new RoleReferenceConverter());
        serializer.Converters.Add(new RelationConverter());
        
        // can write

        return serializer;
    }
    
    #endregion
    
}