namespace Backlot.Core.Security;

/// <summary>
/// Level numbers inspired by Unix permissionlevels
/// Write access is controlled by the IRoleRepositories
/// Read access is controlled by the Scenario execution (at the end)
/// Execute access is controlled by Scenario execution (at start) and does count for the role executing the scenario (not the supporting roles. The only need read access).
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// No permissions, can not read, write or execute
    /// </summary>
    None = 0,
    
    // <summary>
    // Define if the role can execute a scenario
    // </summary>
    // not supported, if you can not read you can not execute
    // Execute = 1,
    
    // <summary>
    // Write only, can only be used with an initial write
    // </summary>
    //Write = 2,
    
    // <summary>
    // Define if the role can execute a scenario and if you can write (changes).
    // </summary>
    // not supported, if you can not read you can not write
    //ExecuteWrite = 3,
    
    /// <summary>
    /// Read only
    /// </summary>
    Read = 4,
    
    /// <summary>
    /// Define if the role can execute a scenario and if there is readaccess.
    /// </summary>
    ReadExecute = 5,
    
    /// <summary>
    /// Read and Write permission
    /// </summary>
    ReadWrite = 6,
    
    /// <summary>
    /// Define if the role can execute a scenario and if there is read and write access.
    /// </summary>
    ReadWriteExecute = 7,
}