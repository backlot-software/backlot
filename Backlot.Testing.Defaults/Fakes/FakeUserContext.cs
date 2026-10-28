using Backlot.Core.Security;

namespace Backlot.Testing.Defaults.Fakes;

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