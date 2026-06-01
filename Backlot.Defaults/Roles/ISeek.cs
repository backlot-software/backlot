using Backlot.Core;

namespace Backlot.Defaults.Roles;

/// <summary>
/// Base Seek object refering to the role you want to use this seek for.
/// </summary>
public interface ISeekBase : IRole
{
    /// <summary>
    /// The role you want to use this seek for.
    /// </summary>
    RoleReference For { get; set; }
}

/// <summary>
/// A seek operation used to navigate through revisions
/// </summary>
public interface ISeek : ISeekBase
{
    /// <summary>
    /// Optional: Checksum to start from
    /// Default: Last revisions (when rev), First revision (when fwd)
    /// 
    /// Keep in mind
    /// - When using the first revision you can't go back (rev).
    /// - When using the last (current) revision you can't go forward (fwd).
    /// </summary>
    string StartingPoint { get; set; }
    
    /// <summary>
    /// Optional: total steps to take
    /// Default: maximum steps to end of list.
    /// </summary>
    int Steps { get; set; } 
    
    /// <summary>
    /// Optional:
    /// - fwd = forward
    /// - rev = reverse
    /// Default: rev
    ///
    /// Reverse: Means stepping back in time does give back values to transform to the previous state again.
    /// Forward: Means stepping forward in time, can be used to calculate what has been changed until now.
    /// 
    /// </summary>
    string Command { get; set; }
}