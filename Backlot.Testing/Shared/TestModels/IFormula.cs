using System.Collections.Generic;
using Backlot.Core;
using Backlot.Core.Json;
using Backlot.Defaults.Instructing;

namespace Backlot.Testing.Shared.TestModels;

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

public interface IFormulaGroup : IPersist
{
    IEnumerable<IFormula> Formulas { get; set; }
}