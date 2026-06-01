
using Backlot.Core;
using Backlot.Core.Security;

namespace Backlot.Experimental.Services.SqlDb.Relational.Experimental;

/// <summary>
/// Initializer for meta data when using acting presents of backlot framework.
/// </summary>
public static class MetaDataInitializer
{
    internal static IPersist Initialize(IPersist role, IDictionary<string, object> origin)
    {
        
        role.LastModified = DateTimeOffset.UtcNow;
        role.DefinePermission(Permission.Create(PermissionLevel.ReadWriteExecute).ToString());

        return role;
    }
}