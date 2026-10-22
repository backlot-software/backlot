using System.ComponentModel.DataAnnotations;
using Backlot.Authentication.Bearer;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Defaults.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Backlot.Authentication.BuiltIn;

/// <summary>
/// Internal login to create a token for the user which can be used for a one time email authentication url.
/// It does make use of bearer authentication.
/// </summary>
[Scenario(typeof(Login), access: [Access.Open])]
public class Login(IUser role, IUserRepository userRepository, ITokenStore tokenStore)
    : Scenario<Login, IUser, bool>(role)
{
    /// <summary>
    /// the "final jwt token", for the request IAuth request.
    /// </summary>
    public string Token { get; set; }
    
    // todo: for backwards compatibility; public string UrlWithToken => $"{Role.RedirectUrl}?token={Token}&env={RootUrl}";
    
    protected override async Task<bool> ExecAsync()
    {
        /* * * * * * * * * * * * * * * * * * * * * * * * * * * *
         *
         *
         *
         *    EXAMPLE LOGIN FOR CUSTOMERS who like to work with email login provider!
         *    THIS need to turn into a documentation example and not a package to be deployed to
         *    NUGET for the future
         *
         *
         */

        try
        {
            Token = string.Empty;

            var tryGetUser = await userRepository.TryGetUser(Role.UserName);

            if (tryGetUser.success)
            {
                // refresh token, only valid for the lifetime of the access token (JWT).
                var options = BearerOptions.FromConfiguration();
                Token = new SignedTokens(options).Create(
                    SignedTokens.RefreshType,
                    TimeSpan.FromMinutes(45), // todo; can be a configuration too.
                    new Dictionary<string, object>
                    {
                        ["sub"] = tryGetUser.username,
                        ["groups"] = tryGetUser.groups
                    });
            }
        }
        catch (Exception ex)
        {
            Token = string.Empty;
            Logger.LogError(ex, "Exception occured at {Scenario} -->", Reference.Name);
            return false;
        }

        return true;

    }
}

public interface IUser : IRole
{
    [Required]
    [EmailAddress]
    string UserName { get; set; }
}
