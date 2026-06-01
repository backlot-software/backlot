using System;
using System.Diagnostics.CodeAnalysis;

namespace Backlot.Core;

public struct Criteria
{
    public string Field { get; set; }
    
    /// <summary>
    /// Valid values are;
    /// 1 - eq = Equal
    /// 2 - ct = contains
    /// ---
    /// 11 - lt = Less Than
    /// 12 - gt = Greater Than
    /// </summary>
    public string Condition { get; set; }
    
    /// <summary>
    /// Enum representation of the condition, can be used for sorting and filtering.
    /// </summary>
    public ConditionEnum ConditionEnum => Enum.TryParse(typeof(ConditionEnum), Condition, true, out var r) ? (ConditionEnum)r : ConditionEnum.eq; 
    
    /// <summary>
    /// The field value
    /// supported are int, float, decimals and strings.
    /// </summary>
    public object Value { get; set; }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ConditionEnum
{
    eq = 1,
    ct = 2,
    // ---
    lt = 11,
    gt = 12
}