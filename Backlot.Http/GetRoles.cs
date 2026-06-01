using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Services;
using Newtonsoft.Json.Linq;

namespace Backlot.Http;

public static class GetRoles
{
    /// <summary>
    /// When Director is used with a body, the body contain a collection of role(s)
    /// </summary>
    /// <param name="requestBody"></param>
    /// <param name="roleRepository"></param>
    /// <returns></returns>
    /// <exception cref="AggregateException"></exception>
    private static IEnumerable<IRole> FromRequestBody(string requestBody, IPersistedRoleRepository roleRepository)
    {
        var body = JObject.Parse(requestBody); // request body, use JObject to handle collections and values.

        foreach (var roleItm in body)
        {
            var rolename = roleItm.Key; //define the role
            var roleType = Loader.GetRoleByName(rolename);

            if (roleItm.Value is JObject jobjectData)
            {
                if (jobjectData.PresentsType(roleType) is { } role)
                    yield return role;
            }
            else
            {
                if (roleItm.Value is JValue jValue) //value is uid (string)
                {
                    var uid = jValue.Value<string>() ?? string.Empty;

                    if (!string.IsNullOrEmpty(uid))
                    {
                        if (roleRepository.TryGet(uid, roleType, out var role))
                            yield return role;
                    }
                }
                else
                {
                    throw new AggregateException(
                        $"The given role '{roleItm.Key}' is not in the correct format");
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="body"></param>
    /// <param name="rolename"></param>
    /// <param name="roleRepository"></param>
    /// <returns></returns>
    /// <exception cref="BadRequestException"></exception>
    public static async Task<IRole[]> ForPostRequest(Stream body, string rolename, IPersistedRoleRepository roleRepository)
    {
        var roleType = Loader.GetRoleByName(rolename);
        
        using var reader = new StreamReader(body);
        var requestBody = await reader.ReadToEndAsync();

        if (typeof(IDirector).IsAssignableFrom(roleType))
        {
            var roles = FromRequestBody(requestBody, roleRepository).ToArray();
            if (!roles.Any())
            {
                throw new BadRequestException("POST requests for a director role must contain a collection of roles in the body.");
            }

            return roles;
        }
        
        var role = !string.IsNullOrWhiteSpace(requestBody)
            ? requestBody.PresentsType(roleType) ?? Acting.New(roleType)
            : Acting.New(roleType);
        
        return [role];
    }
}