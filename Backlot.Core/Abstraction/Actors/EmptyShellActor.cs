using System.Collections.Generic;

namespace Backlot.Core.Abstraction.Actors
{
    /// <summary>
    /// INTERNAL: The actor used when creating a new role without having an actor available
    /// Used for internal purpose only.
    /// </summary>
    public class EmptyShellActor : Dictionary<string, object>
    {
        //todo: make this a hashtable / dictionary or Exando kind of object, also check .IsNull in that case, because this has to return false when the dictionary is filled.
    }
}