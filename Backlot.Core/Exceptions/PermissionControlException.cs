using System;

namespace Backlot.Core.Exceptions;

public class PermissionControlException : ApplicationException
{
    public PermissionControlException(string message) : base(message)
    {
        
    }
    
    public PermissionControlException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}