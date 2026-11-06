using Backlot.Core.Security;

namespace Backlot.Testing.Defaults.Fakes;

/// <summary>
/// Provides a configurable IUserContext for testing.
/// Simulates a user without performing authentication.
/// Use `FakeUserContext` for tests that require a predefined user. Use another `IUserContext` implementation when testing actual authentication behavior.
/// </summary>
/// <remarks>
/// FakeDefaultDirector registers it automatically as the default IUserContext.
///For custom Directors, you can register your own `IUserContext` implementation in `Registration()`
/// 
/// User properties can be changed to test different
/// identities, groups, claims, and authentication states.
/// 
/// Intialize() does not validate tokens.
/// </remarks>
public class FakeUserContext : IUserContext
{
    public string UserName { get; set; } = "UnitTestUser";

    public string AuthScheme { get; set; } = "Bearer";

    public IDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();

    public string[] Groups { get; set; } = ["Everyone", "Admin"];

    public bool IsAuthenticated { get; set; } = true;
    
    public Task Intialize(string token)
    {
        return Task.CompletedTask;
    }

    public bool IsInGroup(string roleName)
    {
        return Array.Exists(Groups,
            group => string.Equals(group, roleName, StringComparison.OrdinalIgnoreCase));
    }
}