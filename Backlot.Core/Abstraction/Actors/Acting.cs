using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Backlot.Core.Abstraction.Actors.RoleCreation;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.DependencyInjection;

namespace Backlot.Core.Abstraction.Actors
{
    public static partial class Acting
    {
        private const string RefererExpressionEnginePattern = "^(?<engine>.):(?<content>.*)$";
        [GeneratedRegex(RefererExpressionEnginePattern)]
        public static partial Regex RefererExpressionEngineRegex();
        
        // Cache to store the metadata types associated with each Role interface (avoids repetitive reflection)
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> _roleMetadataCache = new();

        // Cache to track which (ImplementationType, MetadataType) pairs have been registered (avoids redundant providers)
        private static readonly ConcurrentDictionary<(Type implementationType, Type metadataType), bool> _registeredMetadata = new();

        
        public static TRole New<TRole>()
            where TRole : IRole
        {
            //todo: check if there is a default actor implementation defined for TRole. using the ServiceLocator.
            // ... if not, use the empty shell Actor.
            var actor = new EmptyShellActor();
            return actor.Presents<TRole>(); // new items always are readwrite execute
        }
        
        // ReSharper disable once InconsistentNaming
        public static IRole New(Type TRole)
        {
            //todo: check if there is a default actor implementation defined for TRole. using the ServiceLocator.
            // ... if not use the empty shell Actor.
            var actor = new EmptyShellActor();
            return actor.PresentsType(TRole); // new items always are readwrite execute
        }

        /// <summary>
        /// we recognise the next actor types;
        /// 1) Self - an actor implementing the TRole itself, so it can act as itself.
        /// 2) Become - an actor not implementing the interface itself; for these a dynamic proxy is creating executing the subject (actor), it adopts all behaviour defined for TRole. So it can act as TRole
        /// -----
        /// For all TRole(s) created, the Instructor for TRole is executed.
        /// </summary>
        /// <typeparam name="TRole">The role type you liked the actor to act as</typeparam>
        /// <param name="actor">Every entity </param>
        /// <param name="instructor">Optional (personal) instructor executed before director defined instructors are executed</param>
        /// <returns></returns>
        public static TRole Presents<TRole>(this object actor, Func<TRole, object, TRole> instructor=null) 
            where TRole : IRole
        {
            // an actor is transformed to a role.
            // the actor given within this function is the actual origin and is deserialized into an actable object (f.e. jobject or dictionary) during "become".
            // The actual actor used within the proxy can be manipulated for several reasons and therefore differs from this actor (the original actor 'origin'). 
            
            var role = Become<TRole>(actor); // adapt
            
            if (instructor != null) // run personal instructor(s)
            { 
                role = instructor(role, actor);
            }
            ServiceLocator.Get<IDirector>().Instruct(role, actor); // ask the director to run defined instructors for this role.

            #region Type Provider Registration
            
            // Get or build the list of metadata types to register for this TRole
            var metadataTypes = _roleMetadataCache.GetOrAdd(typeof(TRole), roleType =>
            {
                var types = new List<Type>();
                // Check the role interface itself
                if (!roleType.GetCustomAttributes(false).OfType<ExcludeValidationAttribute>().Any())
                    types.Add(roleType);
            
                // Check all inherited interfaces
                types.AddRange(roleType.GetInterfaces().Where(i => !i.GetCustomAttributes(false).OfType<ExcludeValidationAttribute>().Any()));
                return types;
            });

            var implementationType = role.GetType();
            foreach (var metaType in metadataTypes)
            {
                // Only add the provider if it hasn't been registered for this specific implementation type yet
                if (_registeredMetadata.TryAdd((implementationType, metaType), true))
                {
                    // Register a metadata provider for a type at runtime,
                    // so that .NET’s type/metadata inspection system (“TypeDescriptor”) will treat
                    // implementationType as if it has extra metadata (typically data-annotation attributes)
                    // coming from a separate “buddy” class (metaType).
                    TypeDescriptor.AddProviderTransparent(
                        new AssociatedMetadataTypeTypeDescriptionProvider(implementationType, metaType),
                        implementationType);
                }
            }
            
            #endregion

            return role;
        }
        
        /// <summary>
        /// Typeless representation of the Presents-T- method.
        /// </summary>
        /// <param name="actor">Every entity.</param>
        /// <param name="tRole">The role type you like the actor to act as</param>
        /// <param name="instructor">Optional (personal) instructor executed before director defined instructors are executed</param>
        /// <returns></returns>
        public static IRole PresentsType(this object actor, Type tRole, Func<IRole, object, IRole> instructor=null)
        {
            try
            {
                object func = null;
                if (instructor != null) // wrap the instructor to a Func<TRole, object, TRole> type.
                {
                    // ReSharper disable once PossibleNullReferenceException; we want to have the exception here because technically the method can not be null.
                    var wrap = typeof(Acting)
                        .GetMethod(nameof(PresentsTypeInstructWrapper), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(tRole);
                    func = wrap.Invoke(null, [instructor]);
                }

                // ReSharper disable once PossibleNullReferenceException; we want to have the exception here because technically the method can not be null..
                var method = typeof(Acting)
                    .GetMethod(nameof(Presents), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(tRole);
                return method.Invoke(null, [actor, func]) as IRole;
            }
            catch(TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        private static Func<TRole, object, TRole> PresentsTypeInstructWrapper<TRole>(Func<IRole, object, IRole> func)
            where TRole : IRole
        {
            return (r, o) => (TRole)func(r, o);
        }

        /// <summary>
        /// Transmit behaviour of an existing role to a new actor only containing the properties of the TDestination.
        /// Check the overload for extensive documentation and more options.
        /// Used when TDestination and the current role type are the same.
        /// </summary>
        /// <param name="role">Allowed for all roles but not of type IPersist</param>
        /// <typeparam name="TDestination">Allowed for all IRole but not IPersist</typeparam>
        /// <returns></returns>
        public static TDestination Transmit<TDestination>(this TDestination role)
            where TDestination : IRole
        {
            return Transmit<TDestination, TDestination>(role);
        }

        public static IRole TransmitType<TRole>(this TRole role, Type tDestination)
            where TRole : IRole
        {
            try
            {
                // ReSharper disable once PossibleNullReferenceException; we want to have the exception here because technically the method can not be null..
                var method = typeof(Acting)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == nameof(Transmit) && m.GetGenericArguments().Count() == 2)
                    .MakeGenericMethod(tDestination, tDestination);
                return method.Invoke(null, [role as object]) as IRole;
            }
            catch(TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// Transmit behaviour of an existing role to a new actor only containing the properties of the TDestination, which then is used to represent the role of TDestination.
        /// It allows "finalizing" objects and using the current state (including calculated properties) in the new destination.
        /// Results are always "Readonly", but can use as value of other Role properties.
        /// -------------------------------
        /// AWARENESS:
        /// - Calculated fields of the origin are used & transmitted to the "new"  actor and only ignored at persisting when they are also marked as calculated at the new destination role
        /// - For safety reasons we do not accept to transmit to an IPersist type.
        /// - Metadata is removed and "rebuild" for the new destination.
        /// - Permission of the destination is always Readonly
        /// </summary>
        /// <param name="role">The origin role</param>
        /// <typeparam name="TRole">The role type behaviour which needs to be transmitted.</typeparam>
        /// <typeparam name="TDestination">The role type of the newly created destination role</typeparam>
        /// <returns>A role based on a new actor.</returns>
        public static TDestination Transmit<TRole, TDestination>(this TRole role) 
            where TDestination : IRole
            where TRole : IRole
        {
            // nl; transmit is zenden, transform is overdragen, we "zenden" de eigenschappen van een rol naar een andere rol, zodat deze eigenschappen overgenomen kunnen worden
            // when TDestination is IPersist do not accept. 
            
            // check TDesination is not IPersist or implements it
            if (typeof(IPersist).IsAssignableFrom(typeof(TDestination)))
                throw new ArgumentException("Transforming to IPersist is not supported because transforming is losing skills");

            var names = typeof(TDestination).GetFieldInfo().Select(p => p.Name);

            var dictionary = typeof(TRole)
                .GetFieldInfo().Select(fld => fld.UnderlyingInfo)
                .Where(p => names.Any(n => n == p.Name))
                .ToDictionary(p => p.Name, p => p.GetValue(role, null));
            
            var res = dictionary.Presents<TDestination>();
            return res;
        }
        
        private static TRole Become<TRole>(object content) 
            where TRole : IRole
        {
            // if it is already a proxied role, take the proxied actor as the origin, otherwise the actor it sell
            var origin = content is IProxiedRole pr ? pr.Actor : content;
            
            if (origin == null) throw new ArgumentNullException(nameof(origin));
            
            if (origin is TRole ret) // _self
                return ret;
            
            foreach(var creator in Loader.AllRoleCreatorsSorted)
            {
                if (creator.CanCreate<TRole>(origin)) // the origin becomes the actor
                {
                    return creator.Create<TRole>(origin, // Create the role based on the actor
                        false); // overrule default CanCreate; we already know it can create.
                }
            }
            
            throw new ArgumentException($"There is no implementation of {nameof(IRoleCreator)} defined for the origin with type {origin.GetType().Name}.");
        }
    }
}
