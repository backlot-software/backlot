using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Security;
using Microsoft.Extensions.Logging;
// ReSharper disable SuspiciousTypeConversion.Global Mixins

namespace Backlot.Core.Services
{
    public abstract class BasePersistedRoleRepository :
        IPersistedRoleRepository
    {
        protected static ILogger<IPersistedRoleRepository> Logger => ServiceLocator.GetLog<ILogger<IPersistedRoleRepository>>();
        
        public abstract void Terminate(string key);

        public abstract void FlushDb();

        public abstract IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null);

        public abstract IEnumerable<T> GetAll<T>(int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null) where T : IPersist;

        public abstract Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> refereces, bool includeNoAccess = false);

        public abstract IEnumerable<Revision> GetRevisions<TR>(string uid) where TR : IPersist;
        public abstract bool TryGetPermission(string uid, out Permission permission);

        /// <summary>
        /// Is persisted in current state?
        /// </summary>
        /// <param name="current"></param>
        /// <param name="stored">null when nothing is in the database, the database entity when there is one (also when not in current state).</param>
        /// <typeparam name="TRole"></typeparam>
        /// <returns></returns>
        protected bool IsPersisted<TRole>(TRole current, out TRole stored) where TRole : IPersist
        {
            stored = default;
            
            if (TryGetType(current.Uid, 
                    typeof(TRole), // it's important to use TRole so current and Stored are both presented as the same TRole and not forcing Stored is going to be presented as something it maybe never can be which can cause data loses.
                    out var s))
            {
                stored = (TRole)s; 
                if (current.GetChecksum() == stored.GetChecksum())
                    return true;
            }

            return false;
        }
        
        protected abstract Task<TRole> Store<TRole>(TRole role) where TRole : IPersist;

        public async Task<(bool IsSuccess, TRole Result)> TryPersistResult<TRole>(TRole obj) where TRole : IPersist
        {
            try
            {
                var role = await Persist(obj);
                return (true, role);
            }
            catch (PermissionControlException)
            {
                return (false, default);
            }
        }

        /// <summary>
        /// Try persisting the given role.
        /// When this fails false is returned
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="TRole"></typeparam>
        /// <returns>True when permission is exeucted without any exceptions</returns>
        public async Task<bool> TryPersist<TRole>(TRole obj) where TRole : IPersist
        {
            try
            {
                await Persist(obj);
            }
            catch (PermissionControlException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Build a persistance function using default execution path.
        /// An Exception is thrown when permissions are not correct. To avoid handling the exception, use TryPersist.
        /// </summary>
        /// <param name="obj">The given role</param>
        /// <typeparam name="TRole"></typeparam>
        /// <returns>The merged and updated role</returns>
        /// <exception cref="PermissionControlException">Is thrown when the current user does not have sufficient rights to alter this role</exception>
        public async Task<TRole> Persist<TRole>(TRole obj) where TRole : IPersist
        {
            // check if there is any permission set for the given role, otherwise ignore persistance, to prevent the object can not be read or write by anyone any more in the future.
            
            // for all roles where the masklevel is none an error has to be thrown.
            // an error needs to be thrown also, when there is any user or group level is defined, but none of these levels is higher than none
            if(obj.Permission().MaskLevel == PermissionLevel.None || // when mask level blocks, an error needs to be thrown.
               ((obj.Permission().UserLevels.Any() || obj.Permission().GroupLevels.Any()) && // when mask level does not block, everything is fine as long as there are no user or group levels defined.
                // when there are group or user levels defined, at least one of them needs to have a permission level higher than None.
                !(obj.Permission().UserLevels.Any(u => u.Value > PermissionLevel.None) || obj.Permission().GroupLevels.Any(g => g.Value > PermissionLevel.None))))
                throw new PermissionControlException(
                    $"No permissions defined for this'{obj.GetType().GetRoleName()}'.");
            
            //Check if object already persisted before in this state, persistance not needed
            if (IsPersisted(obj, out var stored))
            {
                //merge not needed because stored and obj are equal.
                obj.LastModified = stored.LastModified;
                return obj;
            }

            if (stored == null) //not persisted in current state + no database entity available.
                return await Store(obj);

            //Persisted in a different state, please check if you have write access on the database entity.
            if (!stored.CanWrite())
                throw new PermissionControlException("The object is already persisted before but in the current context you do not have sufficient permissions to update it.");
            
            //Merge object with object from database, new values are leading
            if (obj is IProxiedRole proxiedObj && stored is IProxiedRole proxiedStored) // both are IProxied then combine.
            {
                proxiedObj.Interceptor.CombineActor(proxiedStored);
            }
            else if (obj is IProxiedRole || stored is IProxiedRole) // one is IProxiedRole then the othre is not and throw an argument exception.
            {
                throw new ArgumentException("Either both or neither of the roles should implement IProxiedRole. You are using a mix of origin types.");
            }

            
            //lastmodified is taken care of in Store implementation.

            //Store the object
            return await Store(obj);
        }

        public bool TryGet<T>(string uid, out T obj) where T : IPersist
        {
            var ret = TryGet(uid, typeof(T), out var oobj);
            obj = (T)oobj;
            return ret;
        }

        public bool TryGet(string uid, Type objType, out IRole obj)
        {
            if (TryGetType(uid, objType, out obj))
            {
                if (obj is IPermission p && string.IsNullOrEmpty(p.__Permission))
                    throw new ApplicationException(
                        $"Integration fault inside {GetType().Name}, the Permission is not set while getting Role '{objType.GetRoleName()}' from the repository. Please fix this within '{GetType().FullName}'");
                
                if (obj.CanRead())
                {
                    // log objects found, for debugging and performance analysis purposes.
                    Logger.LogDebug("Found '{Uid}' Within '{Clss}.{Fn}'", uid, nameof(BasePersistedRoleRepository),
                        nameof(TryGet));
                    return true;
                }
            }
            
            Logger.LogDebug("Not found or not readable '{Uid}' Within '{Clss}.{Fn}'", uid, nameof(BasePersistedRoleRepository), nameof(TryGet));

            obj = null;

            return false;
        }

        /// <summary>
        /// Required implementation for the specific database represented by this repository
        /// TryGetType has to return a role with permissions when 'true'
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="objType"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected abstract bool TryGetType(string uid, Type objType, out IRole obj);
    }
}
