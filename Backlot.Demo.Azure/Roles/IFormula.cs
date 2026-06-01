using System.ComponentModel.DataAnnotations;
using Backlot.Core;
using Backlot.Core.Json;
using Backlot.Defaults.Instructing;

namespace Backlot.Demo.Azure.Roles;

public interface IFormula : IPersist
{
    [Alias(["Op" , "operatie", "op", "Operatie"])] // case sensitive!
    string Operation { get; set; }
    int Number1 { get; set; }
    int Number2 { get; set; }
    
    [Calculated]
    int Outcome { get; set; }
}

/// <summary>
/// Example of a forumule having annotations for validation.
/// </summary>
public interface IFormulaValidation : IFormula
{
    [Required]
    [Range(200,500)]
    new int Number1 { get; set; }
    [Range(1,100)]
    new int Number2 { get; set; }
    
    //email regex
    [RegularExpression(@"^([\w\-]+)@([\w\-]+)((\.(\w){2,3})+)$")]
    string Email { get; set; }
}