#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Services;
using Microsoft.Extensions.Logging;

namespace Backlot.Core.Abstraction.Scenarios
{
    public static class ScenarioBuilder
    {
        private static ILogger<IScenario> Logger => ServiceLocator.GetLog<ILogger<IScenario>>();
        
        /// <summary>
        /// Construct a scenario
        /// </summary>
        /// <param name="sceneref"></param>
        /// <param name="role">The main role for this scenario</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        internal static IScenario Construct(ScenarioReference sceneref, IRole role)
        {
            //todo: cache builded scenarios in a request based cache.
            
            var namecollection = sceneref.Name.Split(".");
            var sceneName = namecollection[0];
            var named = namecollection.Length > 1 ? namecollection[1] : null;
            
            var scene = Loader.GetScenario(sceneName, role);

            if (scene is MethodInfo method)
                return Construct(method, role);
            
            if(scene is Type type)
                return Construct(type, role, named);

            throw new NotFoundException($"Scenario '{sceneref.Name}' not found for role '{role.GetType().Name}'. Possible causes are; your request does not contain a role, or your request contains multiple roles and you have to use the director.");
        }

        /// <summary>
        /// Construct a scenario
        /// </summary>
        /// <param name="sceneref"></param>
        /// <param name="roles">All participating roles</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        internal static IScenario Construct(ScenarioReference sceneref, IEnumerable<IRole> roles)
        {
            var namecollection = sceneref.Name.Split(".");
            var sceneName = namecollection[0];
            var named = namecollection.Length > 1 ? namecollection[1] : null;
            
            MemberInfo? scene = null;

            var roleArray = roles as IRole[] ?? roles.ToArray();
            foreach (var role in roleArray)
            {
                scene = Loader.GetScenario(sceneName, role);

                if (scene != null && scene is Type)
                    break;
            }
            
            if(scene != null && scene is Type st)
                return Construct(st, roleArray, named);

            throw new NotFoundException($"Scenario '{sceneref.Name}' not found. Possible causes are; unknown scenario name, missing or unknown roles used.");
        }
        
        internal static IScenario Construct(Type scenarioType, IRole role, string? named)
        {
            return Construct(scenarioType, [role], named);
        }

        internal static IScenario Construct(Type scenarioType, IEnumerable<IRole> input, string? named)
        {
            var roleRepository = ServiceLocator.Get<IPersistedRoleRepository>();
            var roles = input as IRole[] ?? input.ToArray();
            
            Logger.LogDebug("Construct a class scenario using {ScenarioType} and {@Roles}, within '{Clss}.{Fn}'",
                scenarioType.FullName,
                roles.Select(n => n.GetFriendlyReference()),
                nameof(ScenarioBuilder),
                nameof(Construct));

            if (!scenarioType.IsClass)
            {
                var msg = $"Can not construct a scenario of {scenarioType.FullName}. It is not a class";
                throw new ArgumentException(msg);
            }

            try
            {
                var constructor = scenarioType
                    .GetConstructors()
                    .MaxBy(c => c.GetParameters().Length); //always select the constructor with the most parameters.

                var parameters = constructor?.GetParameters() ?? [];

                // ReSharper disable once PossibleNullReferenceException : there is always a constructor.
                var constRoleParams = parameters //Get all roles used by the constructor.
                    // where a parameter is from a type implementing IRole
                    .Where(r => r.ParameterType.GetInterfaces().Any(i => i == typeof(IRole)))
                    .ToArray();

                if (!string.IsNullOrEmpty(named) && parameters.All(p => p.Name != "named"))
                    throw new ArgumentException(
                        $"Scenario {scenarioType.FullName} does not have a parameter named 'named' to pass the named configuration '{named}'");

                var constParams = parameters //intialize the (none role) parameters for the constructor
                    .Skip(constRoleParams
                        .Length) // skip role parameters, the order of the constructor parameters is important and always start with all roles first.
                    .Select(par => 
                        par.Name == "named" ? 
                            named : 
                            ServiceLocator.Get(par.ParameterType))
                    .ToList();

                var i = 0;
                
                var lookupCache = new Dictionary<string, IPersist?>();

                foreach (var constructorParameter in
                         constRoleParams) //intialize the role parameters, based on the given roles
                {
                    // 1. first check if a role is given
                    var parameterRole =
                        roles.FirstOrDefault(itm =>
                            constructorParameter.ParameterType
                                .IsInstanceOfType(
                                    itm)); // f.e. ICalculableOrderCollection.IsInstanceOfType(class CartProxy : ICart ))

                    // 2. when not give, try to load it from a relation
                    if (parameterRole == null)
                    {
                        foreach (var persistedRole in roles.OfType<IPersist>()) // loop through all "predefined"/given roles and see if the parameter is any of those relations.
                        {
                            var relations = ServiceLocator.Get<IRelationRepository>()
                                .GetAll(persistedRole.GetReference())
                                 .Select(reference => GetRole(reference.Uid))
                                .Where(rr => rr != null && rr.Skills().Any(s => s == constructorParameter.ParameterType.GetRoleName())) // exclude roles representing the skill type.
                                .ToArray();

                            if (relations.Length != 0)
                            {
                                if (relations.Length > 1) // are there more then one relations found for this parent / skill combination?
                                    throw new RoleRelationException(
                                        $"To many related roles found for parameter {constructorParameter.ParameterType} not clear which one to select.");

                                parameterRole = relations.First().PresentsType(constructorParameter.ParameterType); // only execute Presenting to the actual type for the only existing parentrelation/skill pair..
                            }

                            if (parameterRole != null) break; // immediatly break foreach, when parameterRole is found.
                        }
                    }
                    
                    if(parameterRole == null && !constructorParameter.IsNullAllowed())
                        throw new ArgumentException($"No (related) role found for parameter {constructorParameter.ParameterType}, you can decorate the parameter with the [{nameof(NullAllowedAttribute)}] attribute to allow emptyshell actors.");

                    parameterRole ??= Acting.New(constructorParameter.ParameterType);

                    if (parameterRole == null) // 3. when still null, throw an exception.
                    {
                        throw new RoleRelationException(
                            $"No (related) role found for parameter {constructorParameter.ParameterType}, and not possible to create it from an emptyshell.");
                    }

                    constParams.Insert(i, parameterRole); // add the role as the first parameter.
                    i++;
                }

                var instance = Activator.CreateInstance(scenarioType, constParams.ToArray()) as IScenario;
                var manager = ServiceLocator.Get<IConfigurationManager>();
                manager.ResolveConfiguration(instance, named);

                // RETURN -->
                return instance ?? throw new ApplicationException($"Scenario {scenarioType.FullName} could not be constructed.");
                // <-- RETURN
                
                IPersist? GetRole(string uid) // get a role from the repository, cache the result to avoid multiple lookups.
                {
                    if (lookupCache.TryGetValue(uid, out var cachedRole)) return cachedRole;

                    var result = roleRepository.TryGet<IPersist>(uid, out var relatedRole)
                        ? relatedRole
                        : null;

                    lookupCache[uid] = result;
                    return result;
                }
            }
            catch (Exception ex) 
                when (ex is 
                          NullReferenceException or 
                          ArgumentNullException or 
                          InvalidOperationException)
            {
                var basemessage = ex.Message;
                basemessage += "Things you can check: " +
                               "1) Do you serve scenario executions with actors which can act as the defined roles? " +
                               "2) Are there relations stored of the 'not given' roles, so we can define them based on already stored entities? " +
                               "3) Is the constructor of the scenario pulbic? " +
                               "4) Do you configured the servicelocator correctly? ";

                throw new ArgumentException(basemessage, ex);
            }
        }

        /// <summary>
        /// Construct func scenarios
        /// </summary>
        /// <param name="func"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        private static IScenario Construct(MethodInfo func, IRole role)
        {
            Logger.LogDebug("Construct a Func scenario using {MethodInfo} and {Role}, within '{Clss}.{Fn}'",
                func.Name,
                role.GetFriendlyReference(),
                nameof(ScenarioBuilder),
                nameof(Construct));

            if (!func.IsGenericMethod)
            {
                var msg = $"The given func '{func.Name}' is not a generic method";
                throw new ApplicationException(msg);
            }

            return new FuncScenario<IRole, object>(role, _ => //Func scenario factory
            {
                var methodParams = new List<object?>()
                {
                    role
                };

                //safe programming, while official scenario functions cannot have more than 1 parameter, we make sure the code does not break here when it does contain more than 1.
                for (var i = 1;
                     i < func.GetParameters().Length;
                     i++) // make sure you do match the function signature by adding null parameters
                {
                    methodParams.Add(null);
                }

                return func.Invoke(null, methodParams.ToArray()) ?? throw new ApplicationException($"The given func '{func.Name}' can not be invoked.");
            }, func);
        }
    }
}