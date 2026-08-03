using Backlot.Core.Security;

namespace Backlot.Demo.Console;

public class UserCtx : IUserContext
{
    private static string UserNameStatic => "John@doe.com";

    public string AuthScheme => "Bearer";


    public Task Intialize(string token)
    {
        return Task.CompletedTask;
    }

    public string UserName => UserNameStatic;

    public IDictionary<string, string> Claims => new Dictionary<string, string>();

    public string[] Groups => ["Everyone"];

    public bool IsAuthenticated => true;

    public bool IsInGroup(string rolename)
    {
        if (rolename == "Everyone") return true;
        if (rolename == "Admin") return true;

        return false;
    }
}