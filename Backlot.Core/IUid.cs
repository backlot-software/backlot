using System.ComponentModel.DataAnnotations;

namespace Backlot.Core;

/// <summary>
/// Roles which are uniquely identified.
/// A Uid is an overall unique identifier for an entity, with which the system knows how to initialize it.
/// Uid(s) are also used to manage the relations between roles (internal and external).
/// </summary>
public interface IUid : IRole 
{
    /// <summary>
    /// unique id
    /// Uid(s) need to be defined by object constructors or need to have a PUBLIC setter, otherwise deserialization problems occur.
    /// </summary>
    [Required]
    string Uid { get; }
}