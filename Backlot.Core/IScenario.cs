using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Core
{
    public interface IScenario
    {
        /// <summary>
        /// The scenario information contaiing tags, access rights and more
        /// </summary>
        IScenarioInfo Info { get; }
        
        /// <summary>
        /// Each Scenario has at least one role.
        /// Thats also known as the Main Role.
        /// </summary>
        IRole Role { get; }
        
        /// <summary>
        /// All roles including the "main role"..
        /// AWARE: Roles which are (or have become) null are not returned.
        /// </summary>
        IEnumerable<IRole> Roles { get; }
        
        object ResultValue { get; }
        Task Start();

        /// <summary>
        /// Validate if this scenario is allowed to be executed in this context.
        /// </summary>
        /// <returns></returns>
        bool Validate();

        /// <summary>
        /// Event which indicates which (other) event is succesfully executed.
        /// Aware; default implementation is executing this async inside it's own scope.
        /// </summary>
        event AsyncEventHandler<ScenarioEventArgs> Fired;
        
        /// <summary>
        /// Event fired before anything else is executed. When an exception occurs nothing happens after.
        /// Used for checking authentication or other checks that need to be done before executing.
        /// Not allowed to be executed async in its own thread, it needs to be awaited
        /// </summary>
        event AsyncEventHandler<EventArgs> Before;
        
        /// <summary>
        /// Event fired after anything else is executed. When an exception occurs nothing happens after.
        /// Used commiting / closing scope.
        /// Not allowed to be executed async in its own thread, it needs to be awaited
        /// </summary>
        event AsyncEventHandler<EventArgs> After;
        
        ScenarioReference Reference { get; }
    }

    public interface IScenario<out TResult>
    {
        TResult ResultValue { get; }
        Task Start();
    }
    
    public interface IScenario<out TRole, out TResult> : IScenario<TResult>
        where TRole : IRole
    {
        /// <summary>
        /// Each Scenario has at least one role.
        /// Thats also known as the Main Role.
        /// </summary>
        TRole Role { get; }
    }
}