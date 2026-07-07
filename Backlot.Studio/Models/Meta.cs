namespace Backlot.Studio.Models;

public static class Meta
{
    
    /// <summary>
    /// All roles displayed by the UI do always have at least these default types and can therefor be ignored.
    /// </summary>
    public static string[] DefaultRoles => ["Uid", "Role", "Permission", "Persist"]; 
}