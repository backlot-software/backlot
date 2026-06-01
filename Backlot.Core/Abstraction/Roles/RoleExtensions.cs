using System;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
// ReSharper disable MemberCanBePrivate.Global

namespace Backlot.Core.Abstraction.Roles
{
    /// <summary>
    /// General role extensions
    /// </summary>
    public static class RoleExtensions
    {
        /// <summary>
        /// Returns the roles with the given skill type.
        /// </summary>
        /// <param name="roles">A role collection</param>
        /// <typeparam name="TRole">The skill type</typeparam>
        /// <returns></returns>
        public static IEnumerable<TRole> OfSkill<TRole>(this IEnumerable<IRole> roles)
            where TRole : IRole
        {
            return roles
                .Where(r => r.IsOfSkill<TRole>())
                .Select(s => s.Presents<TRole>());
        }
        
        /// <summary>
        /// Returns true when the role is of the given skill.
        /// </summary>
        /// <param name="role">The role to check</param>
        /// <typeparam name="TRole">The Skill type</typeparam>
        /// <returns></returns>
        public static bool IsOfSkill<TRole>(this IRole role)
            where TRole : IRole
        {
            return role.Skills().Any(s => s == typeof(TRole).GetRoleName());
        }
        
        /// <summary>
        /// Check whether or not this role is an empty/null role.
        /// - can be used for optional roles playing in a scenario.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static bool IsNull(this IRole role)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global : because of dynamicproxy resharper can not see if an object does matches interfaces at runtime.
            if (role is IProxiedRole pr)
            {
                return pr.IsNull();
            }
            
            return false;
        }

        /// <summary>
        /// Create a Role Reference and use the most detailed skill as friendly name for the info (description).
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static RoleReference GetReference(this IUid uid)
        {
            Type friendlyType = null; // type used for the friendly naming, this has to be a class as up to the inheritance tree as possible.

            uid.Skills().ToList().ForEach(skill => //based on the skills we try to find a more specific type.
            {
                if (friendlyType == null) friendlyType = Loader.GetRoleByName(skill); 
                
                if (Loader.TryGetRoleByName(skill, out var skillType))
                {
                    if (friendlyType.IsAssignableFrom(skillType) && // if the current friendlytype can inherit from the skilltype
                        !typeof(IProxiedRole).IsAssignableFrom(friendlyType)) // and its not a proxy
                    {
                        friendlyType = skillType; // then we use this skilltype as friendlytype
                    }
                }
            });
                
            return new RoleReference()
            {
                Uid = uid.Uid,
                Info = $"Role: {friendlyType.GetRoleName()}"
            };
        }
        
        
        /// <summary>
        /// Return a friendly reference of this role.
        /// Can be used for debugging purposes.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static string GetFriendlyReference(this IRole role)
        {
            if (role is IUid uid)
            {
                var r = uid.GetReference();
                // only first 6 chars of the uid also when uid is smaller than 6 chars.
                var id = r.Uid == null ? Uid.Empty() : r.Uid.Length > 6 ? r.Uid.Substring(0, 6) : r.Uid;
                return $"{id}.{r.Info}";
            }

            return $"{role.GetType().GetRoleName()}"; //support for none Uid roles.
        }

        private static IChecksumBuilder _checksumBuilder;
        private static IChecksumBuilder ChecksumBuilder => _checksumBuilder ??= ServiceLocator.Get<IChecksumBuilder>();

        public static string GetChecksum(this IPersist role)
        {
            var checksum = ChecksumBuilder.BuildHash(role);
            return checksum;
        }

        // OBSOLETE: public static void Merge -> use IJProxy.Merge instead, Typed objects are never merged and need to be completely defined at start.


        public static Type RoleType(this IRole role)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global : because of dynamicproxy resharper can not see if an object does matches interfaces at runtime.
            if (role is IProxiedRole proxied)
            {
                return proxied.ProxiedType();
            }

            return role.GetType();
        }
    }
}