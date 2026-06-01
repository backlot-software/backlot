using System;
using Backlot.Core.Abstraction.Roles;

namespace Backlot.Core.Json
{
    /// <summary>
    /// Calculated properties are not saved in a persisted state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CalculatedAttribute : FieldCharacteristicAttribute
    {
        
    }
}