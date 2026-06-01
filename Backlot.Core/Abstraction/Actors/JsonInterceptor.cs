using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Newtonsoft.Json.Linq;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Core.Abstraction.Actors;

internal sealed class JsonInterceptor : BaseInterceptor<JContainer>
{
    /// <summary>
    /// Jsoninterceptor does use a cache to fasten access.
    /// </summary>
    private readonly Dictionary<string, object> _cache;

    internal static TRole Create<TRole>(JContainer actor) where TRole : IRole
    {
        //if (typeof(TRole).IsInterface) // default interfaced role types, need to be proxied.
        //{
            return (TRole)ProxyGeneration.Generator.CreateInterfaceProxyWithoutTarget(
                typeof(TRole), // main type (interface)
                [typeof(IJProxy), typeof(IProxiedRole)], // additional interfaces,
                ProxyGeneration.Options,
                new JsonInterceptor(actor, typeof(TRole)));
        //}
            
        // typed origins - are handled by TypedInterceptor.
    }
    
    private JsonInterceptor(JContainer origin, Type roleType) : 
        base(origin.DeepClone() as JContainer, // the actor is a deepclone of the origin for security reasons. 
            roleType)
    {
        _cache = new Dictionary<string, object>();
        
        // json intercepting does only respect actors not having fields marked as calculated in the role type they represent. This way we do support actors containing "calculated" fields, but we ignore them.
        var calculatedProps = roleType.GetFieldInfo().Where(f => f.Attributes.Any(att => att is CalculatedAttribute))
            .Select(f => f.Name).ToArray();
        
        if (Actor != null) RemoveTokens(Actor, calculatedProps, false); // remove calculated fields from the actor.
        
        // __Permission protection is done in the defense layer, not in the interceptor.
    }

    /// <summary>
    /// The properties that represent the actor. For JProxies this is IJProxy.JActor.
    /// </summary>
    /// <returns></returns>
    protected override string[] ConcatReservedPropertyNamesForActor()
    {
        return
        [
            nameof(IJProxy.JActor),
            $"{nameof(IJProxy)}_{nameof(IJProxy.JActor)}"
        ];
    }
    
    protected override bool TryGet(string alias, Type returnType, out object value)
    {
        if(returnType == null)
            throw new ArgumentException($"returnType is not allowed to be null when TryGet alias '{alias}' in {nameof(JsonInterceptor)}");
        
        if(_cache.TryGetValue(alias, out value))
            return true;
        
        var val = Actor[alias];

        if (val is not JContainer
            && !(typeof(IPersist)).IsAssignableFrom(returnType)
            && (!returnType.IsClass || returnType == typeof(string)))
            // when the value is not representing a "complex object"
        {
            if (returnType != typeof(string) || val != null)
                // when it is representing a value in JSON (string, integer, date, etc). convert it to the system type.
                value = val is JValue jval
                    ? jval.Value != null ? Convert.ChangeType(val, returnType) : null
                    : val;
        }
        else //when it is representing a complex object or when it is a jvalue (string) pointing to the persisted object.
        {
            value =  val is JContainer jcVal
                ? Build(jcVal,
                    returnType) //when the value is a container build the object by using the container 
                : Construct(val,
                    returnType); //when the object is not a container construct it using the "refering" value.
        }
        
        if(value != null) _cache[alias] = value;
        return value != null;
    }

    protected override bool TrySet(string alias, object value)
    {
        if (Actor[alias] == null)
        {
            return false; // return false, when not in the original actor. The BaseInterceptor takes care of it.
        }

        _cache.Remove(alias); // clean cache
        Actor[alias] = value != null ? JToken.FromObject(value) : null; //update actor.

        return true;
    }
    
    protected override void BeforePropertyInterception(IInvocation invocation, string propname)
    {
        if (ReservedPropertyNamesForActor.Contains(propname)) // when propertyname is an actor property
        {                                                       // then make sure the Actor is insync with all complex objects underneath (because, these could have been changed).
            foreach (var itm in _cache)                         // This only need to be done for items which are _cached, because this is an indication (it's touched and that's because it maybe is changed). 
            {                                                   // Alternatively we could implement a more early 'binding' approach where we require `INotifyPropertyChange` is implemented for every Role property
                if (string.IsNullOrEmpty(itm.Key) ||            // BUT; we this implementation is more of a 'late' binding approach.
                    Actor[itm.Key] == null) continue; // When there is not representation of this cached item available in the actor, we ignore it.
                
                Actor[itm.Key] = itm.Value is IJProxy jProxy
                    ? jProxy.JActor // when we like to get the Actor of this property we return that. Keep in mind that within that proxy this method is also called.
                    : itm.Value != null // defensive; 
                        ? JToken.FromObject(itm.Value, Strategy.SerializeSafe) // Serialize the value of the complex type using a Safe strategy.
                        : null;
            }
        }
    }

    protected override string[] Skills()
    {
        var skills = RoleType.GetSkills();
        return skills.Union(Actor[Meta.__Skills]?.Values<string>() ?? []).ToArray();
    }

    /// <summary>
    /// Factory method to create a contrete type (returntype) based on the (json) source.
    /// </summary>
    /// <param name="source">the json object</param>
    /// <param name="returnType">the type you need to create</param>
    /// <returns></returns>
    private static object Build(JContainer source, Type returnType)
    {
        if (returnType.IsClass && source is JObject jObject)
        {
            return Construct(jObject, returnType); 
                //jObject.ToObject(returnType, Strategy.Temp());
        }
        
        if (source is JArray array)
        {
            var childType = returnType.IsGenericType ?
                returnType.GetGenericArguments()[0] :
                typeof(object);

            var list = returnType.IsClass
                ? CreateDefault(returnType) as IList
                : CreateDefault(typeof(List<>).MakeGenericType(childType)) as IList;

            foreach (var itm in array.Children<JToken>())
            {
                list?.Add(Construct(itm, childType));
            }

            return list;
        }

        return Construct(source, returnType);
    }

    /// <summary>
    /// Call the builder construct method.
    /// </summary>
    /// <param name="source">the json object</param>
    /// <param name="returnType">the actual type.</param>
    /// <returns></returns>
    private static object Construct(JToken source, Type returnType)
    {
        if (source == null) return null;

        if (returnType.IsAssignableFrom(typeof(ScenarioReference)))
        {
            return source.ToObject<ScenarioReference>(Strategy.DeSerializeSenarioReferenceOnly);
        }
        
        if (returnType.IsAssignableFrom(typeof(RoleReference)))
        {
            return source.ToObject<RoleReference>(Strategy.DeSerializeRoleReferenceOnly);
        }
        
        if (source is JContainer jContainer) // because we are a jsoninterception, the source is a JContainer, a JToken / string.
        {
            if(returnType.GetInterfaces().Contains(typeof(IRole)))
                return jContainer.PresentsType(returnType); //when the returntype is a role, present it as this role.
            
            // when not a Role, we only can try to create the concrete Type.
            var typeToken = jContainer[Meta.__Construct]?.ToObject<string>(); //use this information to build the type, when available.

            // use the meta-data to create the type, and when not available, use the Return type itself.
            return jContainer.ToObject((typeToken != null ? Type.GetType(typeToken) : returnType) ?? returnType, 
                Strategy.DeSerializeDefault);
        }

        //if not a jcontainer, its a JToken / value / string and than; check if it is a reference to a persisted item, if so, try it again, if not, throw an exception.
        
        // -- this specific check is used when properties are IPersisted Roles and in the json only the id is given.
        /* f.e.
           {
              "Uid": "cart-xxxx",
              "Name": "Shoppinglist",
              "DeliverDate": "20xx-01-xx",
              "Customer" : "765630504dc4abe867c0cf47ce1e1aaa",
              "LineItems": [
             ...  
            }
         */

        //get a persisted object. the source in this case is a Uid.
        if (returnType.GetInterfaces().Contains(typeof(IRole)) && 
            ServiceLocator.Get<IPersistedRoleRepository>()
            .TryGet(source.ToString(), returnType, out var typedobject))
        {
            return typedobject;
        }

        return source.ToString();
    }

    protected override bool TryCombine(JContainer additionalActor)
    {
        if(additionalActor == null) return false;
        
        // Actor is leading in below merge result.
        var jo = additionalActor.DeepClone() as JObject;
        jo?.Merge(Actor, // Step 3; Merge both origins 
            new
                JsonMergeSettings() // is with .Actor is leading when values are in both sources, everything is merged into _actor...
                {
                    MergeArrayHandling = MergeArrayHandling.Replace,
                    PropertyNameComparison = StringComparison.InvariantCulture, //respect upper and lower casing.
                    MergeNullValueHandling = MergeNullValueHandling.Merge, // respect null values when explicitly set to null in current Actor.
                });

        if (jo != null) Actor = jo;

        return true;
    }
    
    protected override string[] GetActorPropertyNames()
    {
        return Actor.Children<JProperty>().Select(p => p.Name).ToArray();
    }


    protected override void AddActorProperty(string alias, object value)
    {
        if(value != null && Actor is JObject jactor)
        {
            jactor.Add(alias, JToken.FromObject(value));
        }
        
        // otherwise ignore, no actor properties are added to objects who do not support property adding.
    }

    private static void RemoveTokens(JToken containerToken, string[] names, bool includeChildValues = true)
    {
        if (containerToken.Type == JTokenType.Object)
        {
            foreach (var child in containerToken.Children<JProperty>().ToList())
            {
                if (names.Any(n => n.Equals(child.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    child.Remove();
                }
                else if (includeChildValues)
                {
                    RemoveTokens(child.Value, names);
                }
            }
        }
        else if (containerToken.Type == JTokenType.Array)
        {
            foreach (JToken child in containerToken.Children().ToList())
            {
                if(includeChildValues) RemoveTokens(child, names);
            }
        }
    }
}