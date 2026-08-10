using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Newtonsoft.Json;

namespace Backlot.Core.Security;

/// <summary>
/// Default permission Initialization.
/// </summary>
public static class PermissionInitialization
{
    // We use generic <T> functions here to make sure these defaults can be used with PresentsType as well.

    /// <summary>
    /// When __Permission is not set ReadWriteExecute (also named allaccess) is set as default for the given IPermission'ized role.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin">Instructors do use the original actor 'origin'</param>
    /// <returns></returns>
    public static T AllAccessInitialization<T>(T role, object origin)
        where T : IRole
    {
        if (role is IPermission permission)
        {

            if (string.IsNullOrEmpty(permission
                    .__Permission)) // if the role is not persisted we set the default permission. 
                // IPersisted roles are handled by the persistence / db layer.
            {
                permission.__Permission = Permission.Create(PermissionLevel.ReadWriteExecute).ToString(); // default
            }
        }

        return role;
    }

    /// <summary>
    /// When __Permission is not set Read is set as default for the given IPermission'ized role.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin">Instructors do use the original actor 'origin'</param>
    /// <returns></returns>
    public static T ReadOnlyInitialization<T>(T role, object origin)
        where T : IRole
    {
        if (role is IPermission permission)
        {

            if (string.IsNullOrEmpty(permission
                    .__Permission)) // if the role is not persisted we set the default permission. 
                // IPersisted roles are handled by the persistence / db layer.
            {
                permission.__Permission = Permission.Create(PermissionLevel.Read).ToString(); // default
            }
        }

        return role;
    }

    /// <summary>
    /// When __Permission is not set ReadExecute is set as default for the given IPermission'ized role.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin">Instructors do use the original actor 'origin'</param>
    /// <returns></returns>
    public static T ReadExecuteInitialization<T>(T role, object origin)
        where T : IRole
    {
        if (role is IPermission permission)
        {

            if (string.IsNullOrEmpty(permission
                    .__Permission)) // if the role is not persisted we set the default permission. 
                // IPersisted roles are handled by the persistence / db layer.
            {
                permission.__Permission = Permission.Create(PermissionLevel.ReadExecute).ToString(); // default
            }
        }

        return role;
    }

    /// <summary>
    /// When __Permission is not set, None (No Access) is set as default for the given IPermission'ized role.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin">Instructors do use the original actor 'origin'</param>
    /// <returns></returns>
    public static T NoAccessInitialization<T>(T role, object origin)
        where T : IRole
    {
        if (role is IPermission permission)
        {

            if (string.IsNullOrEmpty(permission
                    .__Permission)) // if the role is not persisted we set the default permission. 
                // IPersisted roles are handled by the persistence / db layer.
            {
                permission.__Permission = Permission.Create(PermissionLevel.None).ToString(); // default
            }
        }

        return role;
    }

    /// <summary>
    /// When __Permission is not set, Only the current user and the admin group do get all access permissions as default for the given IPermission'ized role.
    /// If no user is authenticated, all users do get all access. (read: default AllAccessInitialization)
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static T CurrentUserAllAccessInitialization<T>(T role, object origin)
        where T : IRole
    {
        if (role is IPermission permission)
        {

            if (string.IsNullOrEmpty(permission
                    .__Permission)) // if the role is not persisted we set the default permission. 
                // IPersisted roles are handled by the persistence / db layer.
            {

                if (UserContext.Current.IsAuthenticated)
                {

                    // when a user is loggedin all items created are for that user only.

                    permission.__Permission = Permission
                        .Create(PermissionLevel.ReadWriteExecute) // mask
                        .SetUser(UserContext.Current.UserName, PermissionLevel.ReadWriteExecute)
                        .SetGroup("Admin", PermissionLevel.ReadWriteExecute)
                        .ToString(); // default

                }
                else
                {
                    permission.__Permission = Permission
                        .Create(PermissionLevel.ReadWriteExecute) // mask only, because no user is logged in.
                        .ToString(); // default
                }
            }
        }

        return role;
    }

    /// <summary>
    /// Access initiliazer which does load/respect already persisted permissions immediately at initialization.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T DbAccessInitialization<T>(T role, object origin)
        where T : IPersist
    {
        if (string.IsNullOrEmpty(role
                .__Permission)) // when permission is already loaded use that one, try to load it from the database.
                                // aware that this can be a performance hit, but it is the most accurate way to get the permission.
                                // when using an Encrypted permission, the permission is set during role creation. (acting at interception level).
        {
            var repo = ServiceLocator.Get<IPersistedRoleRepository>();

            if (!role.IsNull() && repo.TryGetPermission(role.Uid, out var dbPermission))
            {
                role.__Permission = dbPermission.ToString();
            }
        }
        
        return role;
    }
    
    /// <summary>
    /// Set Permission based on a server side encrypted hash, "re"-sent by the client.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>A role with the permission set or a PermissionControlException when the hashed value contains a none matching uid.</returns>
    public static T EncryptedPermissionInitialization<T>(T role, object origin)
        where T : IPermission, IUid
    {
        if (string.IsNullOrEmpty(role
                .__Permission)) // if permission is already loaded use that one. Otherwise check the origin for a valid encrypted permission.
        {
            var dic = origin as IDictionary;

            if (dic == null && origin is string str && str.IsJson())
                dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(str, Strategy.DeSerializeDefault.Converters.ToArray());

            if (dic != null) // using origin used to initialize the role (which is not equal to role.Actor) here. -- Actor has cleaned up data, like Calculated fields removed.
            {
                var encryptedValue = dic[Meta.__Permission]?.ToString();
                if (encryptedValue != null && encryptedValue.TryDecryptEncryptedPermissionValue(out var permission, out var forUid))
                {
                    if (forUid != role.Uid)
                        throw new PermissionControlException(
                            "HIGH PRIO: Client is trying to use a hashed permission meant for another role!");


                    role.__Permission = permission;
                }
            }
        }

        return role;
    }
    
    private static bool TryDecryptEncryptedPermissionValue(this string value, out string permission, out string forUid)
    {
        permission = null;
        forUid = null;

        try
        {
            var decrypted = ServiceLocator.Get<IEncryptionService>().Decrypt(value);
            var parts = decrypted.Split('/');
            
            if (parts.Length != 3) // system exception, not correctly build encrypted permission.
                return false;
            
            if(!long.TryParse(parts[2], out var validUntil))  return false;
            
            if (DateTimeOffset.FromUnixTimeSeconds(validUntil) < DateTimeOffset.Now)
                return false;
            
            permission = parts[0]; // first part is the permission
            forUid = parts[1]; // second part is the unique id.
            return true;
        
        }
        catch
        {
            return false;
        }
    }
}