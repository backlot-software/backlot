namespace Backlot.Testing.Core;

public class Formula
{
    public string HelloWorld => "HelloWorld";
    public string Operation { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }

    public string Name => "Example which can presents it self as an IFormula Role";

    public string Uid { get; set; } = null!;
}

public class FormulaDifferentAliasName
{
    public string Op { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }

    public string Name => "Example";

    public string Uid { get; set; } = null!;
}

public class FormulaPermission
{
    public string HelloWorld => "HelloWorld";
    public string Operation { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }

    public string Name => "Example which can presents it self as an IFormula Role";

    public string Uid { get; set; } = null!;
    
    public string __Permission { get; set; }
}

public class FormulaCalculatedField
{
    public string HelloWorld => "HelloWorld";
    public string Operation { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }
    public int? Number3 { get; set; }

    public string Name => "Example which can presents it self as an IFormula Role";

    public string Uid { get; set; } = null!;
}

public class FormulaAlias
{
    public string HelloWorld => "HelloWorld";
    public string Op { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }

    public string Name => "Example which can presents it self as an IFormula Role";

    public string Uid { get; set; } = null!;
}