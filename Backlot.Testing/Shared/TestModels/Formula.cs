namespace Backlot.Testing.Shared.TestModels;

public class Formula
{
    public string HelloWorld => "HelloWorld";
    public string Operation { get; set; } = null!;
    public int? Number1 { get; set; }
    public int? Number2 { get; set; }

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