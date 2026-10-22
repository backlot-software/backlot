using Backlot.Core.Security;

namespace Backlot.Authentication.Bearer;

/// <summary>
/// The Implementation of the IUserContext for Bearer authentication.
/// Access tokens are issued by this installation (as a gateway to an external identity provider)
/// and validated by the registered <see cref="ITokenStore"/>.
/// </summary>
public class BearerUserContext(ITokenStore tokenStore) : IUserContext
{
    public string AuthScheme => "Bearer";

    public async Task Intialize(string token)
    {
        try
        {
            var user = await tokenStore.ValidateAccess(token);
            if (user == null)
            {
                IsAuthenticated = false;
                return;
            }

            MapUser(user);
            IsAuthenticated = true;
        }
        catch
        {
            IsAuthenticated = false;
        }
    }

    /// <summary>
    /// Maps the validated token user onto this context. Override to add custom claims or groups.
    /// </summary>
    protected virtual void MapUser(TokenUser user)
    {
        UserName = user.UserName;
        Groups = user.Groups;
        Claims = user.Claims;
    }

    public string UserName { protected set; get; } = "Anonymous";
    public IDictionary<string, string> Claims { protected set; get; } = new Dictionary<string, string>();
    public string[] Groups { protected set; get; } = [];

    public bool IsInGroup(string groupname)
    {
        return Groups
            .Any(r => r.Equals(groupname, StringComparison.CurrentCultureIgnoreCase));
    }

    public bool IsAuthenticated { private set; get; }
}
