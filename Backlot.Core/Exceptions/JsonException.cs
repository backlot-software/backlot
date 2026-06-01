using System;

namespace Backlot.Core.Exceptions;

public class SerializationException : ApplicationException
{
    public readonly object Obj;

    public SerializationException(object obj, string message) : base(message)
    {
        Obj = obj;
    }
    
    public SerializationException(object obj, string message, Exception innerException) : base(message, innerException)
    {
        Obj = obj;
    }
}