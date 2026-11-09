using System;

namespace Backlot.Core.Abstraction.Scenarios;

public class ExceptionEventArgs: EventArgs
{
    public Exception Exception { get; init; }
    
    /// <summary>
    /// True = Exception means the exception was thrown by the implementation of Exec.
    /// False = Exception was thrown anywhere (including the surroundings of the base <see cref="Scenario{TScenario,TRole,TResult}"/>)
    /// </summary>
    public bool Exec { get; init; }
}