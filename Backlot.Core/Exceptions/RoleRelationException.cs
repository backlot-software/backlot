using System;

namespace Backlot.Core.Exceptions;

public class RoleRelationException : ArgumentException
{
    public RoleRelationException(string message) : base(message)
    {
        
    }
    
    public RoleRelationException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}