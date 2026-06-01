using System;

namespace Backlot.Core.Abstraction.Scenarios
{
    /// <summary>
    /// Attribute which you can be used to exclude a role property on a scenario from validation.
    /// Or a complete (role / interface) to be added to the validation context.
    /// This needs to be set on every property which need to be excluded, so the result property as well.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface)]
    public class ExcludeValidationAttribute : Attribute
    {
        
    }
}
