using System;

namespace Backlot.Core.Exceptions;

public class LoaderException : ArgumentException
{
    public LoaderException(string message) : base(message)
    {
        
    }
    
    public LoaderException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}