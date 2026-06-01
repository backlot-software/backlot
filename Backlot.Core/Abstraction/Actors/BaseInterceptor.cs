using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using Backlot.Core.DependencyInjection;

namespace Backlot.Core.Abstraction.Actors;
public abstract class BaseInterceptor<TActor> : IProxyInterceptor
    where TActor : IEnumerable // ensure we only accept dynamic actors.
{
    /// <summary>
    /// Create a default value for the given type.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    protected static object CreateDefault(Type t)
    {
        // using a "proxy" method to Activator.CreateInstance allows us to optimize performance.
        return Activator.CreateInstance(t);
    }
    
    /// <summary>
    /// The origin actor.
    /// </summary>
    protected TActor Actor;
    
    /// <summary>
    /// The proxied role type
    /// </summary>
    protected readonly Type RoleType;
    
    /// <summary>
    /// Fields for properties not having a representation inside the actor.
    /// </summary>
    protected readonly IDictionary<string, object> Backingfields;

    
    private string[] _actorProperties;
    public string[] ActorProperties()
    {
        if (_actorProperties != null) return _actorProperties;
        _actorProperties = GetActorPropertyNames();
        return _actorProperties;
    }
    
    private HashSet<string> _actorPropertySet;
    
    /// <summary>
    /// Contains actor property fast.
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>
    private bool ContainsActorProperty(string alias)
    {
        _actorPropertySet ??= new HashSet<string>(ActorProperties(), StringComparer.Ordinal);
        return _actorPropertySet?.Contains(alias) ?? false;
    } 

    // Backing fields for reserved properties -->
    
    private string _permission;
    private IDictionary<string, string> _referers;
    
    // <-- Backing fields for intercepted properties

    protected BaseInterceptor(TActor actor, Type roleType)
    {
        Actor = actor;
        RoleType = roleType;
        _referers = new Dictionary<string, string>();
        Backingfields = new Dictionary<string, object>();
        
        // defaults;
        ReservedPropertyNamesForActor =
        [
            nameof(IProxiedRole.Actor),
            $"{nameof(IProxiedRole)}_{nameof(IProxiedRole.Actor)}"
        ];
    }

    public void Intercept(IInvocation invocation)
    {
        #region declare
        
        if (Actor == null)
            return;

        var method = invocation.Method;

        if (method.DeclaringType == null)
        {
            invocation.Proceed();
            return;
        }
        
        var propname = method.GetPropertyName();
        
        #endregion

        #region 1) properties
        
        if (propname != null)
        {
            BeforePropertyInterception(invocation, propname);
            
            #region 1.1) reserved properties

            if(propname == nameof(IProxiedRole.Interceptor)) // direct access to the interceptor via the role, for advanced usage only.
            {
                invocation.ReturnValue = this;
                return;
            }
            
            // __Permission is a reserved property and needs to be explicitly set, it's not loaded directly from the actor.
            if (propname == nameof(IPermission.__Permission))
            {
                if (method.ReturnType != typeof(void)) // get
                {
                    invocation.ReturnValue = _permission;
                    return;
                }
                
                //set
                
                _permission = invocation.Arguments[0] as string;
                return;
            }
            
            if (propname == nameof(IProxiedRole.Referrers))
            {
                if (method.ReturnType != typeof(void)) // get
                {
                    invocation.ReturnValue = () => _referers;
                    return;
                }
                
                //set

                if (invocation.Arguments[0] is Func<IDictionary<string, string>> refererFunc)
                {
                    foreach (var refItm in refererFunc()) // try to add the referers when there is already a referer set for this field, that one is leading.
                    {
                        _referers.TryAdd(refItm.Key, refItm.Value);
                    }
                }

                return;
            }

            if (ReservedPropertyNamesForActor.Contains(propname))
            {
                invocation.ReturnValue = Actor;
                return;
            }
            
            // Custom interceptions of the implemented interceptor.
            if (TryInterceptAfterReservedPropertyInvocation(invocation)) return;

            #endregion
            
            var expressionOrAlias = _referers.TryGetValue(propname, out var exp) //the alias is either the property name or a referer to the alias used by the actor.
                ? exp // an expression or alias is found
                : propname;

            #region 1.2) intercepted properties
            
            if (method.ReturnType != typeof(void)) // 1.2.1) get
            {
                if (TryGetWithExpression(expressionOrAlias, method.ReturnType, out var actorValue))
                {
                    if (actorValue != null)
                    {
                        var rt = Nullable.GetUnderlyingType(method.ReturnType) ?? method.ReturnType;
                        
                        // Check if actorValue is a Guid and the return type is string
                        if (actorValue is Guid guidValue && rt == typeof(string)) // todo: create a more generic converter mechanism.
                        {
                            invocation.ReturnValue = guidValue.ToString();
                        }
                        else
                        {
                            // check if actorValue type implements the return type
                            invocation.ReturnValue = rt.IsInstanceOfType(actorValue) ?
                                actorValue :
                                Convert.ChangeType(actorValue, rt);
                        }
                    }
                    else
                    {
                        invocation.ReturnValue = method.ReturnType.IsValueType ? CreateDefault(method.ReturnType) : null;
                    }
                    
                    return;
                }
                else 
                {
                    Backingfields.TryGetValue(expressionOrAlias, out var backingfieldValue);
                    invocation.ReturnValue = backingfieldValue ?? (method.ReturnType.IsValueType ? CreateDefault(method.ReturnType) : null);
                    return;
                }
            }
            else // 1.2.3) set 
            {
                // the expressionOralias is always an alias here
                if (invocation.Arguments.Length != 0 && !TrySet(expressionOrAlias, invocation.Arguments[0]))
                {
                    var value = invocation.Arguments[0];
                    Backingfields[expressionOrAlias] = value;
                }
                
                return;
            }
        }
        
        #endregion
        
        #endregion
        
        #region 2) methods

        if (method.Name == $"{nameof(IProxiedRole.ProxiedType)}" || method.Name == $"{nameof(IProxiedRole)}_{nameof(IProxiedRole.ProxiedType)}")
        {
            invocation.ReturnValue = RoleType;
            return;
        }
        
        if (method.Name == $"{nameof(IProxiedRole.IsNull)}" || method.Name == $"{nameof(IProxiedRole)}_{nameof(IProxiedRole.IsNull)}")
        {
            invocation.ReturnValue = IsNull();
            return;
        }
        
        if (method.Name == $"{nameof(IProxiedRole.Skills)}" || method.Name == $"{nameof(IProxiedRole)}_{nameof(IProxiedRole.Skills)}")
        {
            invocation.ReturnValue = Skills();
            return;
        }

        if (TryInterceptAfterReservedMethodInvocation(invocation)) return;
        
        #endregion
        
        throw new NotImplementedException($"{method.Name} not supported in {GetType().Name}.");
    }

    /// <summary>
    /// Try a native combination of an additional actor with the same type as the current actor.
    /// This is a faster way of merging, in here we can use optimized code for the specific actor type.
    /// It's an optional implementation do not execute base.TryCombine when you implement this.
    /// </summary>
    /// <param name="additionalActor"></param>
    /// <returns></returns>
    protected virtual bool TryCombine(TActor additionalActor)
    {
        // optional
        return false;
    }

    protected abstract void AddActorProperty(string alias, object value);
    
    /// <summary>
    /// combine an additional actor with the current actor.
    /// </summary>
    /// <param name="additional"></param>
    public void CombineActor(IProxiedRole additional)
    {
        if (additional.Actor is TActor && TryCombine((TActor)additional.Actor))
            return;

        
        foreach (var alias in additional.Interceptor.ActorProperties())
        {
            if (!ContainsActorProperty(alias)) // when the property is not yet available 
            {
                if (additional.Interceptor.TryGet(alias, out var value)) // get the value from the additional source
                {
                    AddActorProperty(alias, value); // add the value to the current actor.
                }
            }
        }
    }

    /// <summary>
    /// Does the role represent itself as a default / null?
    /// </summary>
    /// <returns></returns>
    protected virtual bool IsNull()
    {
        return false;
    }

    /// <summary>
    /// Calculate all skills the role has.
    /// </summary>
    /// <returns></returns>
    protected abstract string[] Skills();

    /// <summary>
    /// Executes before any property (reserved, or none reserved) is intercepted.
    /// </summary>
    /// <param name="invocation">The invocation this intercepting is called with</param>
    /// <param name="propname">the propertyname asked for</param>
    protected virtual void BeforePropertyInterception(IInvocation invocation, string propname)
    {
        // by default; do nothing
    }

    /// <summary>
    /// Can be used by the implemented interceptor to intercept a custom property.
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    protected virtual bool TryInterceptAfterReservedPropertyInvocation(IInvocation invocation)
    {
        // default
        return false;
    }
    
    /// <summary>
    /// Can be used by the implemented interceptor to intercept a custom method.
    /// Last Try before throwing a NotImplementedException.
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    protected virtual bool TryInterceptAfterReservedMethodInvocation(IInvocation invocation)
    {
        // default
        return false;
    }

    /// <summary>
    /// Get the value of an actor property
    /// </summary>
    /// <param name="alias">The alias name used by the actor, 90% of the time this is the propertyname of the role as well.</param>
    /// <param name="returnType">The actual type the invocation expect to return.</param>
    /// <param name="value">The actual value which needs to be returned.</param>
    /// <returns>True when the alias is represented by the actor, false if not</returns>
    protected abstract bool TryGet(string alias, Type returnType, out object value);
    
    public bool TryGet(string alias, out object value)
    {
        return TryGet(alias, typeof(object), out value);
    }
    
    /// <summary>
    /// Get the value of an actor property with an expression engine or when no engine is defined the defined alias name.
    /// </summary>
    /// <param name="expressionOrAlias">the expression or the alias</param>
    /// <param name="returnType"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool TryGetWithExpression(string expressionOrAlias, Type returnType, out object value)
    {
        var match = Acting.RefererExpressionEngineRegex().Match(expressionOrAlias);   
        if (match.Success) // an expression engine is defined
        {
            var engine = match.Groups["engine"].Value;
            var content = match.Groups["content"].Value; // the expression content that can be executed by the engine.

            
            var expEngineType = typeof(IExpressionEngine<>).MakeGenericType(returnType);
            var expressionEngines = ServiceLocator.GetAllFor(expEngineType);
            
            var engineInstance = expressionEngines.FirstOrDefault(e => ((IExpressionEngine)e).Engine == engine[0]) as IExpressionEngine;
            if (engineInstance != null)
            {
                var getValue = engineInstance.Execute(content, returnType, Actor);
                if(getValue != null) 
                {
                    value = getValue;
                    return true;
                }
            }
            value = returnType.IsValueType ? CreateDefault(returnType) : null;
            return false;
        }
        else // the expression is an alias name 
        {
            return TryGet(expressionOrAlias, returnType, out value);
        }
    }

    /// <summary>
    /// Set the value at actor level by using the alias name used by the actor.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="value"></param>
    /// <returns>True when the item is set at actor level, false when this value does not have a representation on actor level</returns>
    protected abstract bool TrySet(string alias, object value);
    
    //protected abstract bool TryAddToActor(string alias, object value, Func<string, bool> isTrue);

    protected abstract string[] GetActorPropertyNames();

    /// <summary>
    /// Property names reserved for presenting the actor inside the proxy interface
    /// Basicly this IProxiedRole.Actor but other special proxies such as JProxy can have their own typed properties.
    /// </summary>
    /// <returns></returns>
    protected HashSet<string> ReservedPropertyNamesForActor
    {
        get;
        private init
        {
            field = value;
            var additionalReservedPropertyNames = ConcatReservedPropertyNamesForActor();
            if (additionalReservedPropertyNames == null) return;
            foreach (var name in additionalReservedPropertyNames)
                field.Add(name);
        }
    }

    protected virtual string[] ConcatReservedPropertyNamesForActor()
    {
        // do nothing by default, optional to implement.
        return null;
    }
}