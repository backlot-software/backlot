using Backlot.Core;

namespace Backlot.Defaults.Roles;

/// <summary>
/// A collection of references
/// </summary>
public interface IReferenceCollection : IRole
{
    public IEnumerable<RoleReference> References { get; set; }
}