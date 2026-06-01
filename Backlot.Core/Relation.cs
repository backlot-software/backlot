#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Backlot.Core.Abstraction.Roles;

namespace Backlot.Core;

/// <summary>
/// A relation refers to a relation between 2 roles
/// A relations is born as soon as 2 roles share (a role) in the same scenario
/// combinations are unique. roleid1=xyz, roledid2=abc equals roleid1=abc, roledid2=xyz
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Relation : Tuple<RoleReference, RoleReference>
{
    //public Type[] Types => new [] {Type1, Type2 };

    public override bool Equals(object? obj)
    {
        // ReSharper disable once UseDeconstruction
        if(obj is Relation relation)
        {
            return (Item1.Uid == relation.Item1.Uid || Item1.Uid == relation.Item2.Uid)
                   && (Item2.Uid == relation.Item2.Uid || Item2.Uid == relation.Item1.Uid); 
        }
			
        return base.Equals(obj);
    }
    
    public override int GetHashCode()
    {
        
        /*
         The GetHashCode() method should reflect the Equals logic; the rules are:
         > if two things are equal (Equals(...) == true) then they must return the same value for GetHashCode()
         > if the GetHashCode() is equal, it is not necessary for them to be the same; this is a collision, 
           and Equals will be called to see if it is a real equality or not.
        */
        
        return -1;
    }

    public static bool operator == (Relation x, Relation y)
    {
        return x.Equals(y);
    }

    public static bool operator != (Relation x, Relation y)
    {
        return !x.Equals(y);
    }

    public bool IsRelated(IUid role)
    {
        return Item1.Uid == role.Uid || Item2.Uid == role.Uid;
    }
    
    public bool IsRelated(RoleReference role)
    {
        return Item1.Uid == role.Uid || Item2.Uid == role.Uid;
    }

    /// <summary>
    /// Related Item
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public RoleReference? Related(IUid role)
    {
        return Related(role.GetReference());
    }
    
    public RoleReference? Related(RoleReference role)
    {
        if (Item1.Uid == role.Uid)
            return Item2;
        
        if (Item2.Uid == role.Uid)
            return Item1;

        return null;
    }

    /// <summary>
    /// Get the "other" item.
    /// Get the related rolereference other than the given "parent"
    /// </summary>
    /// <param name="parent">The parent is the the item you already know</param>
    /// <returns>The rolereference other than the one pointing to the given parent item.</returns>
    public RoleReference GetRelatedItem(IUid parent)
    {
        return GetRelatedItem(parent.GetReference());
    }
    
    /// <summary>
    /// Get the "other" item.
    /// Get the related rolereference other than the given "parent"
    /// </summary>
    /// <param name="parent">The parent is the the item you already know</param>
    /// <returns>The rolereference other than the one pointing to the given parent item.</returns>
    public RoleReference GetRelatedItem(RoleReference parent)
    {
        if (Item1.Uid == parent.Uid)
            return Item2;

        return Item1;
    }
    
    private Relation(RoleReference item1, RoleReference item2) : base(item1, item2)
    {
    }

    public static Relation New(RoleReference role1, RoleReference role2)
    {
        if (role1.Uid == role2.Uid)
            throw new ArgumentException("You are trying to create a circular reference.");

        // Ensure the items are ordered by Uid to prevent duplicates!
        var itm1 = string.Compare(role1.Uid, role2.Uid, StringComparison.Ordinal) < 0 ? role1 : role2;
        var itm2 = string.Compare(role1.Uid, role2.Uid, StringComparison.Ordinal) < 0 ? role2 : role1;
        
        var rel = new Relation(itm1, itm2);
        return rel;
    }
    
    public static Relation New (IUid role1, IUid role2)
    {
        return New(role1.GetReference(), 
            role2.GetReference());
    }


}