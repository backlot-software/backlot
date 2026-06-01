using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Demo.Console.Scenarios;

[Scenario(typeof(Calculate), access: [Access.Everyone])]
public class HelloWorld(IDirector role) 
    : Scenario<HelloWorld, IDirector, string>(role)
{
    protected override string Exec()
    {
        return "Hello World!";
    }
}