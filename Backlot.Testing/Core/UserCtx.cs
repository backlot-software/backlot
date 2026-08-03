using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core.Security;

namespace Backlot.Testing.Core;

public class UserCtx : IUserContext
{
    public static string UserNameStatic => "UnitTestUser";

    public string AuthScheme => "Bearer";

    public Task Intialize(string token)
    {
        return Task.CompletedTask;
    }

    public string UserName => UserNameStatic;

    public IDictionary<string, string> Claims => new Dictionary<string, string>();

    public string[] Groups => ["Everyone", "Admin"];

    public bool IsAuthenticated => true;

    public bool IsInGroup(string rolename)
    {
        if (rolename == "Everyone") return true;
        if (rolename == "Admin") return true;

        return false;
    }
}