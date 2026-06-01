using System.Collections.Generic;
using Backlot.Core;
using Backlot.Core.Json;
using Backlot.Defaults.Instructing;

namespace Backlot.Testing.Core;

public interface INumberBase : IRole
{
    int? Number1 { get; set; }
    int? Number2 { get; set; }
}

[FieldInfoAlias(nameof(Uid), ["Id", "m:{{FormulaId}}_{{Name}}!"])]
public interface IFormula : INumberBase, IPersist
{
    [Alias(["Op", "operatie", "op", "Operatie"])] // case sensitive!
    string? Operation { get; set; }

    [Calculated] int? Number3 { get; set; }
}

/// <summary>
/// As IFormula but types of the number1 and number2 property are changed.
/// </summary>
public interface IFormulaTypeChanged : IPersist
{
    string Operation { get; set; }

    string Number1 { get; set; }
    string Number2 { get; set; }
}

public interface IFormulaGroup : IPersist
{
    IEnumerable<IFormula> Formulas { get; set; }
}

public interface IFormulaGroupTyped : IPersist
{
    IEnumerable<Formula> Formulas { get; set; }
}

public interface IFormulaGroupSelf : IPersist
{
    IEnumerable<FormulaSelf> Formulas { get; set; }
}