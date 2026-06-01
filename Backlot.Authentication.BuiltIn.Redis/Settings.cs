using Backlot.Core.Abstraction.Configuration;

namespace Backlot.Authentication.BuiltIn.Redis;

public class Settings
{
    [Configurable]
    public string ServerUrl { get; set; }
}