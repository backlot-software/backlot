using System;

namespace Backlot.Core.Abstraction.Roles;

/// <summary>
/// Used for field characteristics in roles.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class FieldCharacteristicAttribute : Attribute
{
    
}