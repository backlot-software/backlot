#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Backlot.Core.Security;

/// <summary>
/// Permissions are readonly by default. Use ManagePermission to change permission levels of a role.
/// </summary>
public abstract class ReadOnlyPermission
{
    public abstract PermissionLevel MaskLevel { get; }
    public abstract IReadOnlyDictionary<string, PermissionLevel> GroupLevels { get; }
    public abstract IReadOnlyDictionary<string, PermissionLevel> UserLevels { get; }
    
    /// <summary>
    /// Serialized representation of a permission in the format;
    /// "{Digit}:{Owner}:{Group}"
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"m::{(int)MaskLevel}");

        if (GroupLevels.Any())
        {
            builder.Append(",");
            builder.Append(string.Join(",", GroupLevels.Select(x => $"g:{x.Key}:{(int)x.Value}")));
        }

        if (UserLevels.Any())
        {
            builder.Append(",");
            builder.Append(string.Join(",", UserLevels.Select(x => $"u:{x.Key}:{(int)x.Value}")));
        }

        return builder.ToString();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ReadOnlyPermission permission)
        {
            return permission.ToString() == ToString();
        }

        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(MaskLevel, GroupLevels, UserLevels);
    }
}

public class Permission : ReadOnlyPermission
{
    
#pragma warning disable CS8618 : Owner and Group are always initialized.
    private Permission()
#pragma warning restore CS8618
    {

    }
    
    // <typ> (m)ask, (u)ser, or (g)roup,
    // <val> the name of the user or group, empty for masks
    // <per> the permission level, 0-7
    private const string PermissionPattern = //todo: compile regex 
        "(?<typ>[ugm]):(?<val>[^,]*):(?<per>[0-7]{1})";
    
    private PermissionLevel MskLevel { get; set; } //needed to avoid warnings on Hascode generation.
    public override PermissionLevel MaskLevel => MskLevel;

    public override IReadOnlyDictionary<string, PermissionLevel> GroupLevels => GprLevels;
    public override IReadOnlyDictionary<string, PermissionLevel> UserLevels => UsrLevels;
    
    private SortedDictionary<string,PermissionLevel> GprLevels { get; init; }
    private SortedDictionary<string,PermissionLevel> UsrLevels { get; init; }

    #region Factories

    /// <summary>
    /// Creates an "empty" permission where mask level is based on the given permissionlevel
    /// </summary>
    /// <param name="maskLevel"></param>
    /// <returns></returns>
    public static Permission Create(PermissionLevel maskLevel)
    {
        var grp = new SortedDictionary<string, PermissionLevel>();
        var usr = new SortedDictionary<string, PermissionLevel>();
        
        return new Permission
        {
            MskLevel = maskLevel,
            GprLevels = grp,
            UsrLevels = usr
        };
    }
    
    #endregion

    public static bool IsValid(string pattern)
    {
        var reg = Regex.Match(pattern, PermissionPattern);
        return reg.Success;
    }
    
    public static Permission Deserialize(string str)
    {
        if (string.IsNullOrEmpty(str)) return Create(PermissionLevel.ReadWriteExecute);
    
        var reg = Regex.Match(str, PermissionPattern);

        var mask = PermissionLevel.None;
        var groups = new SortedDictionary<string, PermissionLevel>(); 
        var users = new SortedDictionary<string, PermissionLevel>();

        if (reg.Success)
        {
            
            do
            {
                switch (reg.Groups["typ"].Value)
                {
                    case "m" :
                        mask = (PermissionLevel)int.Parse(reg.Groups["per"].Value);
                        break;
                    case "g" :
                        groups.TryAdd(reg.Groups["val"].Value,
                            (PermissionLevel)int.Parse(reg.Groups["per"].Value));
                        break;
                    case "u" :
                        users.TryAdd(reg.Groups["val"].Value,
                            (PermissionLevel)int.Parse(reg.Groups["per"].Value));
                        break;
                }
                
                reg = reg.NextMatch();
                
            } while (reg.Success);

            return new Permission
            {
                MskLevel = mask,
                GprLevels = groups,
                UsrLevels = users
            };

        }

        return Create(PermissionLevel.None); // when a none matching string is used, return blocked.
    }
    
    #region Chain
    
    public Permission SetMask(PermissionLevel level)
    {
        MskLevel = level;
        
        return this;
    }
    
    public Permission SetGroup(string group, PermissionLevel level)
    {
        if(!GprLevels.TryAdd(group, level)) //try add
            GprLevels[group] = level; // or update.
        
        return this;
    }
    
    public Permission RemoveGroup(string group)
    {
        GprLevels.Remove(group);
        return this;
    }
    
    public Permission SetUser(string user, PermissionLevel level)
    {
        if(!UsrLevels.TryAdd(user, level))
            UsrLevels[user] = level;
            
        return this;
    }
    
    public Permission RemoveUser(string user)
    {
        UsrLevels.Remove(user);
        return this;
    }
    
    /// <summary>
    /// Clear all but keep the masklevel.
    /// </summary>
    /// <returns></returns>
    public Permission Clear()
    {
        UsrLevels.Clear();
        GprLevels.Clear();
        return this;
    }
    
    #endregion
}