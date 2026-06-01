using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Actors.RoleCreation;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Exceptions;

namespace Backlot.Core
{
    // The loader helps to load all assemblies and reserved types, such as roles, scenarios and creators
    public static class Loader
    {
        public static void PreLoad() //load the loader, PreLoad all "static cache" at the start
        {
            void LoadAllAssembliesInFolder(string path)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

                var loadedAssemblies =
                    new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName),
                        StringComparer.OrdinalIgnoreCase);

                Parallel.ForEach(Directory.GetFileSystemEntries(path, "*.dll"), file =>
                {
                    AssemblyName assemblyName;

                    try
                    {
                        assemblyName = AssemblyName.GetAssemblyName(file);
                    }
                    catch (BadImageFormatException)
                    {
                        return;
                    }

                    if (loadedAssemblies.Contains(assemblyName.FullName)) return;

                    try
                    {
                        Assembly.Load(assemblyName);
                    }
                    catch (FileLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                });
            }

            var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            LoadAllAssembliesInFolder(path);
            var _ = RoleNames;
        }

        private static Assembly[] AllAssemblies
        {
            get
            {
                return field ??= LoadAssemblies();

                Assembly[] LoadAssemblies()
                {
                    // to get all assemblies loaded into the current domain,
                    // it's possible the same logical assembly appear more than once when the runtime has actually loaded different instances of it.
                    // to avoid this, distinct by fullname
                    return AppDomain.CurrentDomain.GetAssemblies().DistinctBy(a => a.FullName).ToArray();
                }
            }
        }

        public static Type[] AllTypes
        {
            get { return field ??= AllAssemblies.SelectMany(a => a.GetTypes()).ToArray(); }
        }

        /// <summary>
        /// Get all available scenarios
        /// </summary>
        /// <returns></returns>
        //public static Type GetScenario(IScenarioReference reference)
        //{
        //    return GetScenario(reference.Name, reference.TRole);
        //}

        private static IDictionary<string, MemberInfo> Scenarios { get; set; }

        public static MemberInfo GetScenario(IScenarioInfo scenario)
        {
            return GetScenario(scenario.Name, scenario.TRole);
        }

        internal static MemberInfo GetScenario(string name, IRole role)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var roleType = role is IProxiedRole pr ? pr.ProxiedType() : role.GetType();
            return GetScenario(name, roleType);
        }

        internal static MemberInfo GetScenario(string name, Type roleType)
        {
            if (roleType == null)
                throw new ArgumentException("Can not get Type of the given role in GetScenario.");

            Scenarios ??= new Dictionary<string, MemberInfo>();
            var key = $"{name}*{roleType.FullName}";

            if (Scenarios.TryGetValue(key, out var scenario)) return scenario;

            var methodInfo = AllTypes
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                .FirstOrDefault(m =>
                {
                    var att = m.GetCustomAttributes(false).OfType<IScenarioInfo>().FirstOrDefault();
                    if (att != null)
                    {
                        return att.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
                               att.TRole.IsAssignableFrom(roleType);
                    }

                    return false;
                });

            if (methodInfo != null)
            {
                //var funcScene = Type.MakeGenericSignatureType(typeof(FuncScenario<,>),
                //    (methodInfo.GetCustomAttributes(false).OfType<IScenarioReference>().First()).TRole,
                //    methodInfo.ReturnType);

                //return methodInfo.MakeGenericMethod(role);
                Scenarios[key] = methodInfo.MakeGenericMethod(roleType);
                return Scenarios[key];

            }

            var scene = AllTypes.FirstOrDefault(t =>
            {
                var att = t.GetCustomAttributes(false).OfType<IScenarioInfo>().FirstOrDefault();
                if (att != null)
                {
                    return att.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
                           att.TRole.IsAssignableFrom(roleType);
                }

                return false;
            });

            //return scene;
            Scenarios[key] = scene;
            return Scenarios[key];
        }

        /// <summary>
        /// Get an overview of all scenarios.
        /// </summary>
        /// <returns>Returns scenario names and all allowed roles as an array.</returns>
        public static IEnumerable<IScenarioInfo> GetAllScenarios()
        {
            var references = new List<IScenarioInfo>();

            references.AddRange(AllTypes
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                .Where(m => m.GetCustomAttributes(false).OfType<IScenarioInfo>().Any())
                .Select(m => m.GetCustomAttributes(false).OfType<IScenarioInfo>().First()));

            references.AddRange(AllTypes
                .Where(t => t.GetCustomAttributes(false).OfType<IScenarioInfo>().Any())
                .Select(t => t.GetCustomAttributes(false).OfType<IScenarioInfo>().First()));

            return references;
        }

        /// <summary>
        /// Get all available interfaces for IRole.
        /// </summary>
        /// <returns></returns>
        public static Type[] AllRoles
        {
            get
            {
                if (field != null) return field;

                field =
                    AllTypes
                        .Where(t => t.GetInterfaces().Any(ti => ti == typeof(IRole)) && !t.IsGenericType && !t.IsClass && !t.IsValueType)
                        .ToArray();

                return field;
            }
        }


        /// <summary>
        /// The opposite of GetRoleByName. This one returns the (Role)Name of the given type.
        /// -- Only work for role types
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static string GetRoleName(this Type role)
        {
            if (!typeof(IRole).IsAssignableFrom(role))
                throw new ArgumentException($"{role?.FullName} is not a valid Role implementing the IRole interface.");

            var name = role.Name;
            if (name.EndsWith("Proxy"))
                name = name.Substring(0, role.Name.Length - 5);

            return name.StartsWith("I") ? name.Substring(1) : name;
        }

        private static IDictionary<string, Type> RoleNames
        {
            get
            {
                field ??= AllRoles.ToDictionary(r => r.GetRoleName().ToLower(), v => v);

                return field;
            }
        }

        public static Type GetRoleByName(string name)
        {
            if (TryGetRoleByName(name, out var result))
            {
                return result;
            }

            throw new LoaderException($"Unknown role type '{name}' when '{nameof(GetRoleByName)}'");
        }

        public static bool TryGetRoleByName(string name, out Type type)
        {
            if (name.Equals("role", StringComparison.InvariantCultureIgnoreCase))
            {
                type = typeof(IRole);
                return true;
            }

            var keys = new[]
            {
                (name.StartsWith("I") ? name[1..] : name).ToLower(),
                name.ToLower()
            };

            foreach (var k in keys)
            {
                //Rolenames loaded at preload.
                if (!RoleNames.ContainsKey(k)) continue;

                type = RoleNames[k];
                return true;
            }

            type = null;
            return false;
        }

        /// <summary>
        /// All skills this actor has
        /// </summary>
        /// <param name="role">The actor / role which you like to check the skills for</param>
        /// <returns>All (skilled) roles</returns>
        public static IEnumerable<string> Skills(this IRole role)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global proxied object
            if (role is IProxiedRole proxied)
            {
                return proxied.Skills();
            }

            return role.GetType().GetSkills();
        }

        private static IDictionary<string, IEnumerable<string>> _typeSkills;

        public static IEnumerable<string> GetSkills(this Type roleType)
        {
            _typeSkills ??= new Dictionary<string, IEnumerable<string>>();

            // caching;
            if (roleType.FullName != null && _typeSkills.TryGetValue(roleType.FullName, out var skills1))
                return skills1;

            var interfaces = roleType.GetInterfaces().ToList();
            if (roleType.IsInterface) interfaces.Add(roleType);

            var skills = (from i in interfaces where typeof(IRole).IsAssignableFrom(i) select i.GetRoleName()).ToList();

            if (roleType.FullName != null) _typeSkills[roleType.FullName] = skills;

            return skills;
        }


        /// <summary>
        /// All implementations of IRoleCreator from any assembly used in the current solution.
        /// </summary>
        /// <returns></returns>
        internal static IRoleCreator[] AllRoleCreatorsSorted
        {

            get
            {
                return field ??= AllTypes
                    .Where(t => typeof(IRoleCreator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .Select(rc => CreateSingletonDefaults(rc) as IRoleCreator)
                    .Where(rc => rc != null)
                    .OrderBy(rc => rc.Priority)
                    .ToArray();
            }
        }


        private static readonly Dictionary<Type, object> DefaultValueCache = new();
        
        /// <summary>
        /// Create a default value for the given type.
        /// Based on cached default instaces
        /// Use this instead of Activator.CreateInstance(t) for performance reasons
        /// And only when the instances are meant to be Singletons.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static object CreateSingletonDefaults(Type t)
        {
            if (DefaultValueCache.TryGetValue(t, out var v)) return v;
            DefaultValueCache[t] = Activator.CreateInstance(t);
            return DefaultValueCache[t];
        }
    }
}