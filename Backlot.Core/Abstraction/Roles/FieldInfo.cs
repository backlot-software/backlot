using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Backlot.Core.Abstraction.Roles;

/// <summary>
/// A fieldinfo (characteristic) is a property of a role.
/// The FieldInfo is based on the PropertyInfo and the Characteristics defined in each interface of the role.
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// The name of the underlying property.
    /// </summary>
    public string Name { get; internal init; }
    
    public Type FieldType { get; internal init; }
    
    public PropertyInfo UnderlyingInfo {get; internal init; }
    
    /// <summary>
    /// A concatenation of all attributes defined by interfaces and types of the underlying property.
    /// Attributes used more than one time in the inheritance chain are included that many times.
    /// </summary>
    public IEnumerable<Attribute> Attributes { get; internal init; }

    /// <summary>
    /// Indicates if the underlying property is writable.
    /// </summary>
    public bool CanWrite { get; internal init; }

    /// <summary>
    /// FieldCharacteristics and Validation attributes only.
    /// Attributes used more than one time in the inheritance chain are included once.
    /// </summary>
    public IEnumerable<Attribute> Characteristics =>
        Attributes.Where(a => a.GetType().IsSubclassOf(typeof(ValidationAttribute)) ||
                              a.GetType().IsSubclassOf(typeof(FieldCharacteristicAttribute)))
            .DistinctBy(a => a.GetType().Name);
}