using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Services.LiteDB.Dto;

namespace Backlot.Services.LiteDB;

public static class StoreEntityMetaDataInitializer
{
    internal static TRole Initialize<TRole>(TRole role, StoreEntity entity) where TRole : IPersist
    {
        role.LastModified = entity.LastModified;
        role.DefinePermission(entity.Permission);

        return role;
    }
}