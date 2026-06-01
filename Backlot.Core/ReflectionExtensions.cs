using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;
using Newtonsoft.Json;
using FieldInfo = Backlot.Core.Abstraction.Roles.FieldInfo;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Core
{
    /// <summary>
    /// Internal "Helper" extensions for reflection patterns used when building and intercepting objects.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// When a method is a property method return the clean name
        /// </summary>
        /// <param name="method"></param>
        /// <returns>the propertyname of a propertymethod, but returns null when not a property.</returns>
        public static string GetPropertyName(this MethodInfo method)
        {
            if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
            {
                return method.Name.Substring(4);
            }

            return null;
        }

        public static string ConstructName(this Type type)
        {
            if(!IsEmptyOrAnonymous(type))
                return $"{type?.FullName}, {type?.Assembly.GetName().Name}";

            return string.Empty;
        }


        public static bool IsNumber(this object value)
        {
            return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }

        /// <summary>
        /// Get all public properties and characteristics based on role interface information.
        /// </summary>
        /// <param name="type">Role Type</param>
        /// <param name="includeCalculatedAndIgnored">Optionally define if you like to exclude calculated and ignored properties within the result.</param>
        /// <returns></returns>
        public static IEnumerable<FieldInfo> GetFieldInfo(this Type type, bool includeCalculatedAndIgnored=true)
        {
            var props = (new[] { type })
                .Concat(type.GetInterfaces())
                .SelectMany(i => i.GetProperties())
                .GroupBy(p => p.Name);

            var fields = props.Select(p =>
            {
                var prop = p.First();
                    return new FieldInfo()
                    {
                        Name = p.Key,
                        CanWrite = p.Any(a => a.CanWrite),
                        FieldType = prop.PropertyType, //first is always the first concated (from 'type') in this instance.
                        UnderlyingInfo = prop,
                        // distinct attributes only one per typename
                        Attributes = p.SelectMany(info => info.GetCustomAttributes(false))
                            .OfType<Attribute>(),
                    };
                }).ToList();


            if (includeCalculatedAndIgnored)
                // distinct but get the property with the most attributes as default
                return fields;


            return fields.Where(p => !p.Attributes.Any(c => c is CalculatedAttribute || c is JsonIgnoreAttribute || c is System.Text.Json.Serialization.JsonIgnoreAttribute)).ToList();
        }

        /// <summary>
        /// When the actor is an anonymous a dictionary or it is an emptyshell it has NO constructname
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEmptyOrAnonymous(this Type type)
        {
            return type == null || type == typeof(EmptyShellActor) || type.IsAnonymous() || typeof(IDictionary).IsAssignableFrom(type) || 
                   (type.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))
                ;
        }
        
        /// <summary>
        /// It his an anonymous .net type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAnonymous(this Type type)
        {
            // has a namespace equal to null
            // base type of System.Object
            // IsSealed = true
            // custom attribute 0 is DebuggerDisplayAttribute, Type: ""
            // IsPublic = false
            // -- only checking if it has a namespace is likely be the fastest.
            
            return type.Namespace == null;
        }
        
        /// <summary>
        /// Friendly Type name
        /// Returns rolenames for roles, a c# style generic notation for generics
        /// or the Type.Name as default.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FriendlyName(this Type type)
        {
            if (type == null) return nameof(Object);
            
            return typeof(IRole).IsAssignableFrom(type) ? 
                type.GetRoleName()
                : // else check if type is generic and if so, return the generic type name, otherwise return the type name.
                type.IsGenericType ? $"{type.Name[..^2]}<{string.Join(",", type.GenericTypeArguments.Select(gt => gt.Name))}>" // remove `1 from generic type name and add generic type arguments in c# style.
                    : // default
                    type.Name;
        
        }

        /// <summary>
        /// Returns class + namespace (fullname) without generic information like '1
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string NamespaceName(this Type type)
        {
            var name = type.Name;
            if (type.IsGenericType)
            {
                // remove last 2 chars from name
                name = name[..^2];
            }

            return $"{type.Namespace}.{name}";
        }

        /// <summary>
        /// Get all property names of the actor.
        /// Support for Actors based on JObject, IDictionary or an original typed object.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static string[] ActorProperties(this IRole role)
        {
            return role is IProxiedRole proxy ? 
                proxy.Interceptor.ActorProperties() : // proxied 
                role.GetType().GetFieldInfo().Select(p => p.Name).ToArray(); // this is a self.
        }
        
        public static bool IsNullAllowed(this ParameterInfo parameter)
        {
            // 1. Check for the custom Backlot attribute (fast)
            return parameter.GetCustomAttribute<NullAllowedAttribute>() != null;
        }
    }
}