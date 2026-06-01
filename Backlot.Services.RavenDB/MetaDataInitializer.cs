using Backlot.Core;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Session;

namespace Backlot.Services.RavenDb;

/// <summary>
/// Initializer for meta data when using acting presents of backlot framework.
/// </summary>
public static class MetaDataInitializer
{
    internal static IPersist Initialize(IPersist role, object origin, IMetadataDictionary metadata)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global : mixins using castle proxy.
        if(role is IJProxy jp) RemoveRavenDbSpecificMetadata(jp.JActor, role.RoleType()); // ensure meta data is removed from the actor.
        
        // check if origin is from ravendb
        if(metadata != null)
        {
            if(DateTimeOffset.TryParse(metadata.GetString(Db.LastModified), out var date))
                role.LastModified = date;

            role.DefinePermission(metadata.GetString(Db.Pcl)); 
        }
        else // when not. Ravendb use defaults.
        {
            role.LastModified = DateTimeOffset.UtcNow;
            role.DefinePermission(Permission.Create(PermissionLevel.ReadWriteExecute).ToString());
        }
        
        return role;
    }
    
    /// <summary>
    /// Used to clean up actors during Initialization of roles based on RavenDb entities.
    /// </summary>
    /// <param name="jobject"></param>
    /// <param name="backlotTypeInfo"></param>
    private static void RemoveRavenDbSpecificMetadata(JObject jobject, Type backlotTypeInfo)
    {
        if (jobject == null)
            return;

        if (jobject.ContainsKey("@metadata"))
            jobject.Remove("@metadata");

        if (jobject.ContainsKey("Id") && !backlotTypeInfo.GetProperties()
                .Any(p => p.Name.Equals("Id", StringComparison.InvariantCultureIgnoreCase)))
            jobject.Remove("Id");
    }
}