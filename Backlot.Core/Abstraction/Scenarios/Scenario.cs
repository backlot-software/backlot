using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Exceptions;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Microsoft.Extensions.Logging;
using ValidationException = Backlot.Core.Exceptions.ValidationException;

namespace Backlot.Core.Abstraction.Scenarios
{
    /// <summary>
    /// Base scenario to be used by custom implementation of scenarios.
    /// Implement the Exec or ExecAsync which made a scenario the equivalent of a "static" function with context.
    /// If you like to enable .Play inherit from <see cref="Scenario{TScenario, TRole, TResult}"/> or <see cref="DirectorScenario{TScenario,TResult}"/> instead.
    /// Important notes:
    /// - IPersist Roles are saved automatically.
    /// - IUid roles are automatically persisted as a relation.
    /// - Prevent automatic persistance with the PersistAndRelate boolean.
    /// </summary>
    /// <typeparam name="TRole">The main role responsible for playing this scenario. If you don't have any use the IDirector</typeparam>
    /// <typeparam name="TResult">The result the implementation of Exec will return</typeparam>
    public abstract class Scenario<TRole, TResult> :
        IScenario<TRole, TResult>,
        IScenario
        where TRole : IRole
    {
        public event AsyncEventHandler<EventArgs> Playing;
        public event AsyncEventHandler<EventArgs> Ending;

        public event AsyncEventHandler<ScenarioEventArgs> Fired;

        /// <summary>
        /// Fired before anything. Fired is always fired and before any validation and or permission check.
        /// </summary>
        public event AsyncEventHandler<EventArgs> Before;
        
        /// <summary>
        /// Fired after everything went succesfully, not fired when validation fails.
        /// </summary>
        public event AsyncEventHandler<EventArgs> After;

        private ScenarioReference _reference;

        private static IPersistedRoleRepository Repo => ServiceLocator.Get<IPersistedRoleRepository>();

        // ReSharper disable once MemberCanBePrivate.Global : It's allowed to use the logger in the calling scenarios as well.
        protected static ILogger<IScenario> Logger => ServiceLocator.GetLog<ILogger<IScenario>>();

        public ScenarioReference Reference => _reference ??= BuildReference();

        private IScenarioInfo _info;
        public IScenarioInfo Info
        {
            get
            {
                if (_info != null) return _info;
                
                _info = this is IFuncScenario funcScene
                    ? funcScene.Func.GetCustomAttributes(false).OfType<IScenarioInfo>().FirstOrDefault()
                    : GetType().GetCustomAttributes(false).OfType<IScenarioInfo>().FirstOrDefault();

                if (_info == null)
                {
                    return new NoScenarioInfo<TRole, TResult>(
                        this is IFuncScenario fs ? $"Func.{fs.Func.Name}" : GetType().Name, 
                        GetType(), 
                        [], 
                        GetType().FullName);
                }
                
                return _info;
            }
        }

        public TRole Role { get; private set; }

        [ExcludeValidation]
        public TResult ResultValue { get; private set; }
        object IScenario.ResultValue => ResultValue;
        
        /// <summary>
        /// Validation results optionally filled by the Validate method.
        /// </summary>
        public ICollection<ValidationResult> ValidationResults { get; } = new List<ValidationResult>();

        IRole IScenario.Role => Role;
        
        IEnumerable<IRole> IScenario.Roles => GetRoles().Select(itm => itm.Item2).Where(x => x != null);
        
        protected IDirector Director => ServiceLocator.Get<IDirector>();

        #region Settings
        
        /// <summary>
        /// An option to disable persisting and relating of the roles within this scenario.
        /// </summary>
        protected virtual bool PersistAndRelate => true;
        
        /// <summary>
        /// optional setting to indicate which named settings need to be loaded. (empty by default).
        /// </summary>
        private readonly string _named;
        
        #endregion

        /// <summary>
        /// INTERNAL: Make sure you call initialize manually, when using parameterless constructor.
        /// </summary>
        protected Scenario()
        {
        }

        /// <summary>
        /// DEFAULT: Initialized automatically
        /// </summary>
        /// <param name="role"></param>
        /// <param name="named"></param>
        /// <exception cref="ArgumentException"></exception>
        protected Scenario(TRole role, string named=null)
        {
            _named = named ?? string.Empty;
            
            // finalize
            Initialize(this, role, Director);
        }

        protected static void Initialize(Scenario<TRole, TResult> scenario, TRole role, IDirector director)
        {
            if (role.IsNull())
                throw new ArgumentException(
                    $"'{typeof(TRole).GetRoleName()}' role not defined. The main role is not allowed to be optional.");

            scenario.Role = role;
            
            // Check if the director has defined (customer specific) compositions for this scenario
            director.Compose(scenario);

            #region Bingewatching

            //send current state to bingewatcher
            var bingewatchers = ServiceLocator.GetAllFor<IBingeWatcher>();

            foreach (var bingewatcher in bingewatchers)
            {
                bingewatcher?.Watch(scenario);
            }

            #endregion

            #region Scene watching

            var scenarioType = scenario.GetType();
            var sceneWatcherType = typeof(ISceneWatcher<>).MakeGenericType(scenarioType);
            var scenewatchers = ServiceLocator.GetAllFor(sceneWatcherType);

            foreach (var scenewatcher in scenewatchers)
            {
                var watch = scenewatcher.GetType().GetMethod(nameof(ISceneWatcher<IScenario>.Watch), [scenarioType]);
                watch?.Invoke(scenewatcher, [scenario]);
            }

            #endregion
        } 

        public virtual async Task Start()
        {
            if(Before != null) 
                await Before.Invoke(this, null); //special event not attached to generic fire event
                
            if (!Role.CanExecute()) // check if the role can execute this scenario
            {
                throw new PermissionControlException(
                    $"Main role '{Role.RoleType().GetRoleName()}' does not have sufficient permissions to execute this scenario.");
            }
            
            // all supporting roles need to be "readable" at least.
            foreach (var role in ((IScenario)this).Roles)
            {
                if (!role.CanRead())
                {
                    throw new PermissionControlException(
                        $"Supporting role '{role.RoleType().GetRoleName()}' does not have sufficient permissions to be read by the current user.");
                }
            }
            
            if (Validate())
            {
                Logger.LogInformation("Validation finished while, {Role} will start playing {@Reference}, within '{Clss}.{Fn}'",
                    Role.GetFriendlyReference(),
                    Reference,
                    GetType().Name,
                    nameof(Start));
                
                await OnPlaying(); // Call play event

                Logger.LogInformation("Start exeucting while, {Role} is playing {@Reference}, within '{Clss}.{Fn}'",
                    Role.GetFriendlyReference(),
                    Reference,
                    GetType().Name, 
                    nameof(Start));

                ResultValue = await ExecAsync();

                if(PersistAndRelate)
                    await ExecPersistAndRelate();

                if (ResultValue is IRole roleResult && !roleResult.CanRead())
                {
                    ResultValue = default; // clear result
                    //todo: log warning or throw exception based on configuration
                }

                await OnEnding(); // call ended event
            }
            else
            {
                var msg = $"Can not start because it doesn't match executing criteria. Please check {Reference.Name}.{nameof(ValidationResults)}.";
                Logger.LogInformation("Exception '{ExcType}' will be thrown '{ExcMessage}', while playing {@Reference} with {Role}, within '{Clss}.{Fn}'.'", 
                    nameof(ValidationException),
                    msg,
                    Reference,
                    Role.GetFriendlyReference(),
                    GetType().Name, 
                    nameof(Start));
                
                // validation exception thrown, use either .Validate() before playing the scenario, or fix criteria.
                throw new ValidationException(ValidationResults, msg);
            }
            
            if(After != null)
                await After.Invoke(this, null); //special event not attached to generic fire event
                
            Logger.LogInformation("Finished playing {@Reference} with {Role}, within '{Clss}.{Fn}'",
                Reference,
                Role.GetFriendlyReference(),
                GetType().Name, 
                nameof(Start));
        }

        /// <summary>
        /// Ensure all latest version(s) of each role are saved and ensure all relations between roles.
        /// Roles are only persisted when the Role needed for this scenario is an IPersist Role itself.
        /// F.e. When the object is an IPersist itself and the defined Role (propertyType) for the scenario is not. Than the role is not persisted.
        /// Relations are always saved as long as the object value is OfType IUid.
        /// All Role property values are updated with theire merged version from the database.
        /// </summary>
        private async Task ExecPersistAndRelate()
        {
            var roles = GetRoles().Where(r => r.role != null)
                .ToList();
            
            //todo: potential performance win when using a parallel foreach
            foreach (var itm in roles.Where(t => typeof(IPersist).IsAssignableFrom(t.type))) // only roles defined as IPersist are valid for "saving" even when the value it self is an IPersist we only persist when the role is defined as such.
            {
                var role = (IPersist)itm.role;
                
                var per = await Repo.TryPersistResult(role);
                if (per.IsSuccess)
                {
                    // ensure the latest version of the role is used for all future use of this scenario (f.e. in (mail)watchers etc).
                    itm.set(per.Result);
                }
                else
                {
                    Logger.LogWarning("Persistable {Role} with {Uid} could not be persisted, within '{Clss}.{Fn}'",
                        role.GetFriendlyReference(),
                        role.Uid,
                        nameof(ScenarioBuilder),
                        nameof(ExecPersistAndRelate));
                } // persist latest version of each role
            }

            await EnsureRelations(roles
                .Where(t => typeof(IUid).IsAssignableFrom(t.type)) // filter only roles defined as IUid for this scenario are ensured relation, if not do not relate (even if the value itself is an IUid)
                .Select(t => ((IUid)t.role).GetReference()));
        }
        
        /// <summary>
        /// INTERNAL: Internal use only, does create relations for all IEnumerables given.
        /// </summary>
        /// <param name="itms"></param>
        protected static async Task<IEnumerable<Relation>> EnsureRelations(IEnumerable<RoleReference> itms)
        {
            var result = new List<Relation>();
            var relationRepo = ServiceLocator.Get<IRelationRepository>();
            //var combinations = new List<Relation>();
            var skip = 1;
            
            var uids = itms.ToArray();
            if (uids.Length < 2) return [];

            foreach(var itm1 in uids)
            {
                foreach (var itm2 in uids.Skip(skip))
                {
                    if (itm2.Uid != itm1.Uid)
                    {
                        var relation = Relation.New(itm1, itm2);
                        result.Add(relation);
                        await relationRepo.Add(relation);
                    }
                }
		
                skip++;
            }

            return result;
        }
        
        /// <summary>
        /// By default all roles are validated. You can optionally override this function to add custom validation, or to skip role validation.
        /// When add validation make sure you call base.Validate() to ensure all roles are validated.
        /// When you want to skip role validation, just return a boolean based on your custom validation and optionally fill the ValidationResults as part of the scenario.
        /// </summary>
        /// <returns></returns>
        public virtual bool Validate()
        {
            var isValid = !ValidationResults.Any(); // true by
            foreach (var roleItem in GetRoles().Where(r => r.excludeValidation)) //only validate roles that are marked "include in validation" (not having "[ExcludeValidation]" attribute)
            {
                if (roleItem.role == null)
                {
                    ValidationResults.Add(new ValidationResult($"{roleItem.Item1.GetRoleName()} is null."));
                    return false;
                }

                // create a validation context for the role and do use the MetadataTypeAttribute if defined.
                var context = new ValidationContext(roleItem.role, serviceProvider: null, items: null);
                
                var validation = Validator.TryValidateObject(roleItem.role, context, ValidationResults, true);
                // is valid is false when it is false previously and / or when validation is false
                isValid = isValid && validation;
            }
            
            return isValid;
        }

        /// <summary>
        /// When your scenario is a synchronious scenario override this function.
        /// </summary>
        /// <returns></returns>
        protected virtual TResult Exec()
        {
            Logger.LogInformation("While trying to play {@Reference} with {Role}. A '{ExcType}' is be thrown '{ExcMessage}', within '{Clss}.{Fn}', because 'Exec' is not implemented", 
                Reference,
                Role.GetFriendlyReference(),
                nameof(NotImplementedException),
                "not implemented",
                GetType().FullName, 
                nameof(Exec));
            
            throw new NotImplementedException(
                "No execution method implemented. You need to override Exec() or ExecAsync() without calling the underlying base.Exec() or base.ExecAsync()");
        }

        protected virtual Task<TResult> ExecAsync()
        {
            // When this function is not overriden by the (none synchrone) Exec() need to be executed. 
            // It's allowed to use FromResult in this case. Stephen Cleary; "When you're implementing an interface that allows asynchronous callers, but your implementation is synchronous."
            return Task.FromResult(Exec());
        }

        private async Task OnPlaying()
        {
            await FireAsync(Playing, nameof(Playing));
        }

        private async Task OnEnding()
        {
            await FireAsync(Ending, nameof(Ending));
        }
        
        
        /// <summary>
        /// Fire the given Async Event.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="eventName"></param>
        /// <param name="arguments"></param>
        /// <typeparam name="T"></typeparam>
        // ReSharper disable once MemberCanBePrivate.Global : is used by implementations outside backlot core.
        protected async Task FireAsync<T>(AsyncEventHandler<T> ev, string eventName, T arguments=null)
            where T : EventArgs
        {
            
            Logger.LogInformation("Event {Event} executed with, within '{Clss}.{Fn}'", 
                eventName,
                GetType().FullName, 
                nameof(FireAsync));

            var args = new ScenarioEventArgs
            {
                EventName = eventName
            };

            try
            {
                if (ev != null) await ev.Invoke(this, arguments);

                if (Fired != null)
                {
                    await Fired.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception '{ExcType}' occured '{ExcMessage}', within '{Clss}.{Fn}'. For async event '{Event}'", 
                    ex.GetType().FullName,
                    ex.Message,
                    GetType().Name, 
                    nameof(FireAsync),
                    eventName);
                // todo: make it configurable to throw or not.
                //throw;
            }
        }

        private ScenarioReference BuildReference()
        {
            return new ScenarioReference
            {
                Name = string.IsNullOrWhiteSpace(_named) ? Info.Name : $"{Info.Name}.{_named}"
            };
        }
        
        /// <summary>
        /// All roles within this scenario.
        /// </summary>
        /// <returns>Array of Item1: RoleType in this Role, Item2: The actual value., Item3: Include in validaton?</returns>
        private IList<(Type type, IRole role, Action<object> set, bool excludeValidation)> GetRoles()
        {
            var roles = new List<(Type type, IRole role, Action<object> set, bool excludeValidation)>(); //Tuple<Type, IRole, Action<object>, bool>>();

            var roleProperties = GetType().GetProperties()
                .Where(prop => typeof(IRole).IsAssignableFrom(prop.PropertyType));
            
            
            foreach (var prop in roleProperties)
            {
                var setter = new Action<object>(value => // used by persistandrelate to update the value with the merged result from the repository.
                {
                    if (prop.Name == nameof(Role))
                    {
                        Role = (TRole)value;
                        return;
                    }

                    if (prop.Name == nameof(ResultValue))
                    {
                        ResultValue = (TResult)value;
                        return;
                    }

                    // update all properties having at least a private setter, if not merged results from the repository can not be set.
                    var setter = prop.GetSetMethod(true);
                    setter?.Invoke(this, [value]);
                });
                
                roles.Add((
                    // type:
                    prop.PropertyType,
                    // role:
                    (IRole)prop.GetValue(this, null), // AWARE: null values have to be returned within this list, because they are used for validation.
                    // set
                    setter,
                    // excludeValidation:
                    !prop.GetCustomAttributes(false).OfType<ExcludeValidationAttribute>()
                        .Any() //not when set explicitly on the property
                    && !prop.PropertyType.GetCustomAttributes(false).OfType<ExcludeValidationAttribute>()
                        .Any() // not when set globally on the role itself
                ));
            }

            return roles.ToList();
        }
    }
}
