using System.ComponentModel.DataAnnotations;
using Backlot.Core;

namespace Backlot.Authentication.BuiltIn.Roles;

public interface IAuthBase: IRole
{
    /// <summary>
    /// The UiUrl of the client application.
    /// </summary>
    string UiUrl { get; set; }
}

public interface IAuth : IAuthBase
{
    [Required]
    [EmailAddress]
    string UserName { get; set; }
}