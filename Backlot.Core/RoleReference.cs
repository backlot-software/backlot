using System;
using System.Diagnostics.CodeAnalysis;

namespace Backlot.Core;

/// <summary>
/// A reference for a uniquely identified role
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class RoleReference
{
    
    /// <summary>
    /// Do create RoleReference by using .GetReference() extension method on IRole
    /// </summary>
    internal RoleReference()
    {
        // internal constructor.
    }
    
    /// <summary>
    /// IPersist Uid
    /// </summary>
    public string Uid { get; set; }
    
    /// <summary>
    /// Rolename and/or userfriendly representation of this reference.
    /// For readability purposes only. Can be null during code execution, but should be set when serialized / showed.
    /// </summary>
    public string Info { get; set; }
    
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        
        // ReSharper disable once UseDeconstruction
        if(obj is RoleReference reference)
        {
            return reference.Uid.Equals(Uid, StringComparison.InvariantCulture); 
        }

        return false;
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

    public static bool operator == (RoleReference x, RoleReference y)
    {
        return !ReferenceEquals(null, x) && x.Equals(y);
    }

    public static bool operator != (RoleReference x, RoleReference y)
    {
        return !ReferenceEquals(null, x) && !x.Equals(y);
    }
}