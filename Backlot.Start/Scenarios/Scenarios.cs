using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Start.Scenarios;

/// <summary>
/// This example shows how to create a scenario that's executed by a default role.
/// These scenarios are accessible via a POST request and the body (a JSON object) is the role.
/// </summary>
/// <param name="role"></param>
[Scenario(typeof(Hi), // this attribute allows the scenario to be executed as web request.
    access: [Access.Everyone])] // by everyone who is in this group (Everyone is a reserved group name for everyone who has an account).
public class Hi(IRole role) // use the right order for the parameters. (default role first, then all participant roles, then all interfaces managed by DI).
    : Scenario<Hi, IRole, bool>(role) // inherit from Scenario or DirectorScenario
{
    // [Configurable] // this attribute allows the setting to be configured via the admin panel or anything that inherits; IConfigurationManager
    // public string Setting { get; set; }
    
    // optionally override public override bool Validate() for custom validation.

    protected override bool Exec() // override Exec or ExecAsync, this is where your logic goes.
    {
        // Role can't be null
        return this.Role != null;
    }
}

/// <summary>
/// This example shows how to create a scenario that's executed by the director.
/// These scenarios are only accessible via a GET request and do not have a body.
/// </summary>
/// <param name="role"></param>
[Scenario(typeof(HiDirector), access: [Access.Everyone])]
public class HiDirector(IDirector role)
    : DirectorScenario<HiDirector, string>(role)
{
    protected override string Exec()
    {
        // In this example role is always an IDirector
        return this.Role.GetType().Name;
    }
}