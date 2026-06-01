namespace Backlot.Core.Security;

public static class Access
{
    /// <summary>
    /// Virtual group for everyone authenticated. It's not needed to add users to this group.
    /// Users authenticated are automatically added to this group.
    /// Users not authenticated are not in this group and thus not allowed to execute scenarios marked as "Everyone"
    /// </summary>
    public const string Everyone = "Everyone";
    
    /// <summary>
    /// Reserved groupname for administrators. Users need to be specificly added to this group via user management.
    /// Users marked as "SystemAdmin" are automatically added to this group.
    /// </summary>
    public const string Admin = "Admin";
    
    /// <summary>
    /// Wildcard used to mark scenarios as "Open"
    /// Scenarios marked with "Open" are accessible for anyone, even users not authenticated.
    /// </summary>
    public const string Open = "*";

}