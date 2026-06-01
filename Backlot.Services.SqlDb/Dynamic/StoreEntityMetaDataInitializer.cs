using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Services.SqlDb.Dto;

namespace Backlot.Services.SqlDb.Dynamic;

public static class StoreEntityMetaDataInitializer
{
    internal static TRole Initialize<TRole>(TRole role, StoreEntity entity) where TRole : IPersist
    {
        role.LastModified = entity.LastModified;
        role.DefinePermission(entity.Permission);

        return role;
    }
}