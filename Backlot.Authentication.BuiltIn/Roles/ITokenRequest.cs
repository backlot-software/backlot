using System.ComponentModel.DataAnnotations;
using Backlot.Core;

namespace Backlot.Authentication.BuiltIn.Roles;

public interface ITokenRequest : IRole
{
    /// <summary>
    /// A valid not expired refresh token.
    /// </summary>
    [Required]
    string? RefreshToken { get; set; }
}
