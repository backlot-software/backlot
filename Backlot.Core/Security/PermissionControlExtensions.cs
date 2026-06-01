using System;
using System.Linq;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;

namespace Backlot.Core.Security;

public static class PermissionControlExtensions
{
    /// <summary>
    /// INTERNAL: Defines a permission for a persisted role.
    /// Can only be used by Repositories to define the initial permission based on whats currently in the database.
    /// Not allowed to be used elsewhere and therefor only needed for IPersisted roles.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="pattern"></param>
    /// <exception cref="PermissionControlException"></exception>
    public static void DefinePermission(this IPersist role, string pattern)
    {
        if (!Security.Permission.IsValid(pattern))
            throw new PermissionControlException($"The pattern given '{pattern}' is not a valid parmission pattern. Because {nameof(DefinePermission)} is only " +
                                                 $"allowed to be called within persistance layer please fix there, or remove the use of this code.");
        
        if(!string.IsNullOrEmpty(role.__Permission))
            throw new PermissionControlException(
                $"You tried '{nameof(DefinePermission)}' for a role. The apis are meant to be used 'internal' and for persistance layers" +
                $"The given Role '{role.RoleType().FriendlyName()}' already has a permission set. " +
                $"For safety reasons it's not allowed to 're-create' a new permission.");

        role.__Permission = pattern;
    }
    
    /// <summary>
    /// The permissions for this role
    /// Make sure the permission is set, otherwise an exception is thrown
    /// </summary>
    /// <param name="role"></param>
    /// <returns>
    /// - The deserialized representation of the permission, or readwriteexecute for none permissionized roles.
    /// - Throws an PermissionControlException when no permission is set. Ensure the permission is set.
    /// </returns>
    public static ReadOnlyPermission Permission(this IRole role)
    {
        return role.WritablePermission();
    }

    /// <summary>
    /// All permissions are calculated based on the WritablePermission.
    /// However the writable permission is only meant to be used internally or via the ManagedPermission method.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    /// <exception cref="PermissionControlException"></exception>
    private static Permission WritablePermission(this IRole role)
    {
        if (role is IPermission rr)
        {
            if (string.IsNullOrEmpty(rr.__Permission))
                throw new PermissionControlException(
                    $"Role '{role.RoleType().FriendlyName()}' is an '{nameof(IPermission)}'ized role but the __Permission is not set. Is Present<T> executed for this role?");
            
            return Security.Permission.Deserialize(rr.__Permission); // return the deserialized version of the role
        }

        // when the role is not a IPermissionized role, return the default permission, which means NoRestrictions.
        return Security.Permission.Create(PermissionLevel.ReadWriteExecute); //the default for not permissionized roles is "NoRestrictions"
    }

    public static void ManagePermission(this IRole role,
        Action<Permission> action)
    {
        var permission = role.WritablePermission();
        action(permission);
        
        if(role is IPermission pr)
            pr.__Permission = permission.ToString();
    }
    
    /// <summary>
    /// Can the current user read the details of this role.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public static bool CanRead(this IRole role)
    {
        var level = role.CurrentUserPermissionLevel();

        return level is PermissionLevel.Read 
            or PermissionLevel.ReadExecute 
            or PermissionLevel.ReadWrite 
            or PermissionLevel.ReadWriteExecute;
    }

    /// <summary>
    /// Can the current user update this role.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public static bool CanWrite(this IRole role)
    {
        var level = role.CurrentUserPermissionLevel();

        return level is PermissionLevel.ReadWrite 
            or PermissionLevel.ReadWriteExecute;
        //or PermissionLevel.Write 
        //or PermissionLevel.ExecuteWrite 
    }
    
    /// <summary>
    /// Can the current user execute scenarios with this role.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public static bool CanExecute(this IRole role)
    {
        var level = role.CurrentUserPermissionLevel();
        
        return level is PermissionLevel.ReadExecute 
            or PermissionLevel.ReadWriteExecute;
        //or PermissionLevel.Execute
        //or PermissionLevel.ExecuteWrite 
    }
    
    /// <summary>
    /// Permission level for the current user on this role.
    /// Mask level is always leading when it has lower permissions than the user or group level
    /// than user level counts, no matter if it is higher or lower than groups the user is in.
    /// than the highest group level the user is in counts.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public static PermissionLevel CurrentUserPermissionLevel(this IRole role)
    {
        var user = UserContext.Current;
        var permission = role.Permission();

        if (!permission.GroupLevels.Any() && !permission.UserLevels.Any())
            return permission.MaskLevel; // mask level is required, if nothing else is set, masklevel is used as the permissionlevel.

        if (!user.IsAuthenticated) // if not authenticated return public / wildcard level or the default mask level;
        {
            if (permission.GroupLevels.TryGetValue("*", out var pub) && pub <= permission.MaskLevel)
                return pub;

            return PermissionLevel.None;
        }
            
        // if authenticated 
        
        // 1) first check if there is a specific permission set for this user;
        if (permission.UserLevels.TryGetValue(user.UserName, out var usr))
        {
            if(usr <= permission.MaskLevel) 
                return usr; // if the user level is lower than the mask level, return the user level.
            
            // if the user level is higher than the mask level, return the mask level.
            return permission.MaskLevel;
        }

        // 2) then check if there is a specific permission set for the users groups and if so? return the highest level, but no higher than the mask level;

        var grp = PermissionLevel.None; // default;
        if (permission.GroupLevels.TryGetValue("*", out var wildcardlvl))
            grp = wildcardlvl; // when wildcard level is found, thats the default;
        
        foreach (var group in permission.GroupLevels)
        {
            if (user.IsInGroup(group.Key) && group.Value > grp)
            {
                grp = group.Value;
            }

            if(grp > permission.MaskLevel) 
                return permission.MaskLevel; // stop searching if we have the highest level.
        }

        return grp;
    }

    /// <summary>
    /// Encrypts the permission object to be used as a client side cached value which can not be manipulated.
    /// Using a encrypted permission is a way to ensure you can skip a database call to set the permission at request.
    /// </summary>
    /// <param name="permission">The permission object needs to have a Uid</param>
    /// <param name="validForMinutes">standard is 15 minutes</param>
    /// <returns></returns>
    internal static string EncryptedPermissionString(this IUid permission, int validForMinutes = 15)
    {
        
        // format: permission/uid/validuntil AWARE when changed other depending code such as the decrypt initialization must be changed as well.
        return  ServiceLocator.Get<IEncryptionService>().Encrypt($"{permission.Permission()}/{permission.Uid}/{DateTimeOffset.Now.AddMinutes(validForMinutes).ToUnixTimeSeconds()}");
    }
}