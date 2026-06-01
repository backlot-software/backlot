using System;

namespace Backlot.Core.Abstraction.Scenarios
{
    public class ScenarioEventArgs: EventArgs
    {
        public string EventName { get; init; }
    }
}
