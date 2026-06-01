using System;
using Backlot.Core.Json;

namespace Backlot.Core
{
    /// <summary>
    /// An IPersist can be anything, from a product to a location
    /// from a location to a person. On it self the IPersist is a Role as well.
    /// Persist roles are persisted automatically by the Scenario base class and managed by the PersistedRoleRepository.
    /// </summary>
    public interface IPersist : IPermission, IUid 
    {
        /// <summary>
        /// Free name refering to something logically for your domain.
        /// Name(s) need to be defined by object constructors or need to have a PUBLIC setter, otherwise deserialization problems occur.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Calulcated metafield set by the database of choice.
        /// Setting this is a responsibility of the database repository, do not forget to implement it there.
        /// DO NOT SET THE LastModified elsewhere.
        /// </summary>
        [Calculated]
        DateTimeOffset? LastModified  { get; set; }
        
        
    }
}