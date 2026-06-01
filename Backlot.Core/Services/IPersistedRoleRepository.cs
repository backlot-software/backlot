using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core.Security;

namespace Backlot.Core.Services
{
    /// <summary>
    /// Roles marked as IPresisted can be "stored" / persisted.
    /// </summary>
    public interface IPersistedRoleRepository
    {

        /// <summary>
        /// Try to persist and ignore exceptions.
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="TRole"></typeparam>
        /// <returns>Returns succeeded and the final 'role' result.</returns>
        Task<(bool IsSuccess, TRole Result)> TryPersistResult<TRole>(TRole obj) where TRole : IPersist;
        
        /// <summary>
        /// Tries to persist and ignore exceptions.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="role"></param>
        /// <typeparam name="TRole"></typeparam>
        /// <returns>Returns false when persist does not succeed</returns>
        Task<bool> TryPersist<TRole>(TRole obj) where TRole : IPersist;
        
        /// <summary>
        /// (Longterm) save / persist the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Throws exception when persist does not succeed</returns>
        Task<TRole> Persist<TRole>(TRole obj) where TRole : IPersist;

        /// <summary>
        /// Try to get the raw saved serialized object
        /// </summary>
        /// <param name="uid">The saved Uid of the object</param>
        /// <param name="objType">The object type</param>
        /// <param name="obj">The result object</param>
        /// <returns></returns>
        bool TryGet(string uid, Type objType, out IRole obj);

        bool TryGet<T>(string uid, out T obj)
            where T : IPersist;

        /// <summary>
        /// Terminate an object.
        /// </summary>
        /// <param name="key"></param>
        void Terminate(string key);
        
        /// <summary>
        /// Flush complete DB.
        /// </summary>
        void FlushDb();

        /// <summary>
        /// GetAll persisted roles of type objType and matchting the given criteria
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="total"></param>
        /// <param name="criteria"></param>
        /// <param name="from"></param>
        /// <param name="till"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        IEnumerable<IRole> GetAll(Type objType, 
            int page, 
            int pageSize, 
            out int total, 
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null,
            DateTimeOffset? till = null,
            string orderby = null);

        /// <summary>
        /// GetAll persisted roles of type T and matchting the given criteria
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="total"></param>
        /// <param name="criteria"></param>
        /// <param name="from"></param>
        /// <param name="till"></param>
        /// <param name="orderby"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> GetAll<T>(
            int page, 
            int pageSize, 
            out int total, 
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null,
            DateTimeOffset? till = null,
            string orderby = null) where T : IPersist;

        
        /// <summary>
        /// Get roles in bulk matching one the of references.
        /// -- Does return the entity no matter if canread is true or false as long as this entity is handled by the implemented repository.
        /// -- The implementation of this need to follow permission guidelines. It's advisable to return dummy objects for objects the currentUser does not has access to (only name, uid and lastmodified).
        /// </summary>
        /// <summary>
        /// Get Items in
        /// </summary>
        /// <param name="refereces">The list of references, you like to load. Depending on the implementation it depends what the max size of this array will be.</param>
        /// <param name="includeNoAccess">Indication if you like to load dummy objects for the entities you don't have access to</param>
        /// <returns></returns>
        Task<IEnumerable<IPersist>> GetBulk(
            IEnumerable<RoleReference> refereces,
            bool includeNoAccess = false);

        /// <summary>
        /// Get all available revisions for the given role.
        /// Ordered by the current revision first.
        /// </summary>
        /// <typeparam name="TR">An IPersisted role</typeparam>
        /// <returns>A list of full copies of each revision</returns>
        IEnumerable<Revision> GetRevisions<TR>(string uid) where TR : IPersist;
        
        /// <summary>
        /// Try to get the permission only for an object. This can be used when you only like to get the permission corresponding to a uid
        /// without executing any other acting / presenting / initialization. The implementation on repository level can use fast indexes or caches for it
        /// depending on the implementation.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        bool TryGetPermission(string uid, out Permission permission); 
    }
}
