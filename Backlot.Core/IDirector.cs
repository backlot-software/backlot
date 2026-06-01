using System;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;

namespace Backlot.Core;

/// <summary>
/// Special role object of the director, directing the "application" and all scenarios.
/// The IDirector is registered as a singleton.
/// This one is especially usefull for scenarios who don't have actual roles playing the scenario it self
/// like "view scenarios; get all, get (the one and only) etc..
/// </summary>
[ExcludeValidation]
public interface IDirector : IRole
{
    /// <summary>
    /// File system need to be defined before initialization of a director
    /// Ideally this is a singleton.
    /// </summary>
    [Json.Calculated] // ignore, not allowed to be part of a request.
    IFileSystem FileSystem { get; }
    
    /// <summary>
    /// Configuration manager need to be defined before initialization of a director
    /// Ideally this is a singleton.
    /// </summary>
    [Json.Calculated] // ignore, not allowed to be part of a request.
    IConfigurationManager ConfigurationManager { get; }
    
    /// <summary>
    /// Used during scenario initialization to add missing "compositions" to the scenario
    /// </summary>
    /// <param name="scenario"></param>
    void Compose(IScenario scenario);
    
    /// <summary>
    /// Used during actor presenting to instruct the role further.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="origin">The origin, in this case not the same as the actor. RoleCreators do turn origins into accesable actors (like typed objects, dictionaries or json objects) this origin is the real origin used, most of the time a the raw string.</param>
    /// <typeparam name="TRole"></typeparam>
    void Instruct<TRole>(TRole role, object origin) where TRole : IRole;
    
    /// <summary>
    /// 2nd backlot function called by startup pipeline to initialize the director.
    /// </summary>
    void Incept();
    
    /// <summary>
    /// Initializing container registration and needs to be executed before Incept
    /// </summary>
    void Registration();
}

public abstract class Director(IFileSystem fileSystem, IConfigurationManager configurationManager)
    : IDirector
{
    
    public IFileSystem FileSystem { get; } = fileSystem;
    public IConfigurationManager ConfigurationManager { get; } = configurationManager;

    public void Compose(IScenario scenario)
    {
        var composerType = typeof(Composer<>).MakeGenericType(scenario.GetType());
        var composer = ServiceLocator.Get(composerType);
        
        if (composer is IComposer c)
        {
            c.Compose(scenario);   
        }
    }

    public void Instruct<TRole>(TRole role, object actor)
        where TRole : IRole
    {
        var index = 0;
        var delegates = new List<(int,Delegate)>();
        
        foreach (var skill in role.Skills()) // get all instructors for all skills
        {
            if(Loader.TryGetRoleByName(skill, out var sr))
            {
                // get all instructors for this skill type
                var instructorType = typeof(Instructor<>).MakeGenericType(sr);
                var instructors = ServiceLocator.GetAllFor(instructorType);
                
                foreach (var instructor in instructors) // loop through all instructors and execute the instruct Func
                {
                    // invoke the instruct Func
                    var instructProp = instructor.GetType().GetProperty(nameof(Instructor<TRole>.Instruct))?.GetValue(instructor);
                    // ReSharper disable once PossibleNullReferenceException : not possible because of this is handled within Instructor object.
                    var priority = (int)instructor.GetType().GetProperty(nameof(Instructor<TRole>.Priority))?.GetValue(instructor);

                    if (instructProp is Delegate instruct)
                    {
                        delegates.Add((priority == 0 ? index : priority, instruct));
                        if(priority == 0) index++;
                    }
                }
            }
        }
        
        foreach (var result in delegates.OrderBy(d => d.Item1))
        {
            try                                                                                                                      
            {                                                                                                                        
                role = (TRole)result.Item2.DynamicInvoke(role, actor);                                                                   
            }                                                                                                                        
            catch (ArgumentException e)                                                                                              
            {                                                                                                                        
                //-- likely this error can be skipped                                                                                
                //throw new ApplicationException(                                                                                    
                //    $"An instructor is tried to be executed but is not compatible with the given role '{role?.GetType()}'.");      
                // todo: or make this a configurable warning..                                                                       
                // do nothing -- only execute instrcutors which are compatible with the defined role                                 
            }                                                                                                                        
        }
    }

    public abstract void Registration();
    /// <summary>
    /// 2nd function called during director initialization.
    /// </summary>
    public abstract void Incept();

    /// <summary>
    /// Define all scenes with the given bingewatcher
    /// Add optional configuration. When the implementation does use a configuration file this file is always leading
    /// </summary>
    /// <typeparam name="TWith">The bingewatcher</typeparam>
    protected abstract void WatchAll<TWith>(Action<TWith> configure=null)
        where TWith : class, IBingeWatcher, IWatcher;

    /// <summary>
    /// Define which scene can be watch by which watcher
    /// Add optional configuration. When the implementation does use a configuration file this file is always leading
    /// </summary>
    /// <typeparam name="TScene">The scene to watch</typeparam>
    /// <typeparam name="TWith">The watcher which will watch the scene</typeparam>
    protected abstract void Watch<TScene, TWith>(Action<TWith> configure=null)
        where TScene : IScenario
        where TWith : class, ISceneWatcher<TScene>, IWatcher;

    /// <summary>
    /// At the start of the application, the director can assign instructors for each role
    /// </summary>
    /// <param name="instructor"></param>
    /// <typeparam name="TRole"></typeparam>
    protected abstract void AssignInstructorFor<TRole>(Func<TRole, object, TRole> instructor)
        where TRole : IRole;

    /// <summary>
    /// At the start of the application, the director can assign instructors for each role. optional priority is used to determine the order of execution.
    /// </summary>
    /// <param name="instructor"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TRole"></typeparam>
    protected abstract void AssignInstructorFor<TRole>(Func<TRole, object, TRole> instructor, int priority)
        where TRole : IRole;

    /// <summary>
    /// At the start of the application, the director can assign an expression engine for each data type
    /// </summary>
    protected abstract void AssignExpressionEngineFor<T, TE>()
        where TE : class, IExpressionEngine<T>;

    /// <summary>
    /// Called by program.cs to prepare the director to guide the application.
    /// </summary>
    /// <param name="guide"></param>
    /// <typeparam name="TScene"></typeparam>
    protected abstract void PrepareCompositionFor<TScene>(Action<TScene> guide)
        where TScene : IScenario;
}