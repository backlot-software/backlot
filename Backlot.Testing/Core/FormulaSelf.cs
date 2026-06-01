using System;
using Backlot.Core;
using Backlot.Core.Json;

namespace Backlot.Testing.Core;

public class FormulaSelf : IFormula
{
    public string Operation { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }
    [Calculated]
    public int? Number3 { get; set; }

    public string Name => "Example which is a role it self";

    public DateTimeOffset? LastModified {get;set;}

    public string Uid { get; set; } = null!;

    // make sure "m::7 is returned when permission is not filled in
    string? IPermission.__Permission { get; set; }
}