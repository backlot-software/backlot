using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backlot.Core.Security;
using Backlot.Defaults.Services;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Backlot.Authentication.Basic;

/// <summary>
/// The Implementation of the IUserContext for basic authentication.
/// Basic authentication does check against a database of users for every request and therefor can come with a small performance drop.
/// It's also less safe than the BuiltIn variant, because it is using the same token for every request.
/// </summary>
public class BasicUserContext(IUserRepository userRepository, IEncryptionService encryptionService) : IUserContext
{
    public string AuthScheme => "Basic";
    
    public string Token
    {
        set => Intialize(value).ConfigureAwait(false);
    }

    private async Task Intialize(string token)
    {
        try
        {
            // Decode the token
            var data = Convert.FromBase64String(token);
            var decodedString = Encoding.UTF8.GetString(data);

            // Basic Auth tokens are usually in the format "username:password"
            var parts = decodedString.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length > 0)
            {
                var un = parts[0];
                var pw = parts[1];

                var userResult = await userRepository.TryGetUser(un);
                if (userResult.success)
                {
                    var settings = JObject.Parse(userResult.settings);
                    
                    if (encryptionService.Hash(pw) == settings["pw"]?.ToString())
                    {
                        UserName = userResult.username;
                        Groups = userResult.groups;
                        IsAuthenticated = true;

                        return;
                    }
                }

                IsAuthenticated = false;
            }
        }
        catch
        {
            IsAuthenticated = false;
        }
    }
    
    public string UserName { private set; get; } = "Anonymous";
    public IDictionary<string, string> Claims => new Dictionary<string, string>() { {"sub", UserName}, { "groups", string.Join(",", Groups) } };
    public string[] Groups { private set; get; } = [];

    public bool IsInGroup(string groupname)
    {
        return Groups
            .Any(r => r.Equals(groupname, StringComparison.CurrentCultureIgnoreCase));
    }

    public bool IsAuthenticated { private set; get; }
}