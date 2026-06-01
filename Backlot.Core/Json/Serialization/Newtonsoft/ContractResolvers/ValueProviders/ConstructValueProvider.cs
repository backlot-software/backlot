using System;
using Backlot.Core.Abstraction.Actors;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ValueProviders;

internal class ConstructValueProvider : IValueProvider
{
    public object GetValue(object value)
    {
        if (!(value is IRole)) return null;

        if (value is IProxiedRole proxied)
        {
            return null;
        } 
        
        // not proxied is _self

        return value.GetType().ConstructName();
    }

    public void SetValue(object target, object value)
    {
        throw new NotImplementedException("SetValue not implemented for fixed properties; set JsonProperty.Writable = false.");
    }
}