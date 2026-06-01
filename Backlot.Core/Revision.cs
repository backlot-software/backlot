namespace Backlot.Core;

/// <summary>
/// Revision (step) track / chapter of a role.
/// A role evolves during time. Revisions are the states of a role at a given time.
/// </summary>
public class Revision
{
    
    /// <summary>
    /// The Reference to the corresponding role
    /// </summary>
    public RoleReference Reference { get; set; }
    
    /// <summary>
    /// The checksum calculated at the time of this revision
    /// </summary>
    public string Checksum { get; init; }

    /// <summary>
    /// The state the role had after this revision.
    /// </summary>
    public IRole Content { get; init; }
}