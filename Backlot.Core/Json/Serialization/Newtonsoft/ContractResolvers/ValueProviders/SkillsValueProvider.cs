using System;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ValueProviders;

internal class SkillsValueProvider : IValueProvider
{
    public object GetValue(object target)
    {
        if(target is IRole role)
            return role.Skills().ToArray();

        return null;
    }

    public void SetValue(object target, object value)
    {
        throw new NotImplementedException("SetValue not implemented for fixed properties; set JsonProperty.Writable = false.");
    }
}