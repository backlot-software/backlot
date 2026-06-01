using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backlot.Authentication.BuiltIn.Roles;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;
using Backlot.Defaults.Services;

namespace Backlot.Authentication.BuiltIn.Scenarios
{
    /// <summary>
    /// Creates an access token with the same jti as the "parent" refresh token.
    /// Access tokens are ideally shortly lived and used to access resources using the IUserContext.
    /// </summary>
    [Scenario(typeof(Access), access: [Core.Security.Access.Open])] // todo: add rate limiting for scenarios like this
	public class Access(
        ITokenRequest role,
        JwtTokenService tokenService,
        ITokenRepository tokenRepository,
        IConfigurationManager configurationManager,
        IUserRepository userRepository)
        : TokenRequestScenarioBase(role, tokenService, tokenRepository, configurationManager)
    {
        [Configurable] public override int TokenDurationInMinutes { get; set; } = 5;
        [Configurable] public string CreateNewRefreshToken { get; set; } = "false";

        protected override async Task<Response> ExecAsync()
        {
            var tryGetUser = await userRepository.TryGetUser(RefreshToken.UserName); // try to get users to set a group claim.

            if (tryGetUser.success)
            {
                var createNewToken = Convert.ToBoolean(CreateNewRefreshToken);
                var response = createNewToken ? await Play(Role) : new Response();
                if(createNewToken) RefreshToken = new TokenHandlerBase(TokenService, response.RefreshToken);
                
                var token = TokenService.CreateAccessToken(new Dictionary<string, string>
                    {
                        { TokenService.ClaimNames.Id, RefreshToken.Claims[TokenService.ClaimNames.Id] },
                        { TokenService.ClaimNames.Username, RefreshToken.UserName },
                        { TokenService.ClaimNames.Groups, tryGetUser.groups.Aggregate((current, next) => $"{current},{next}") }
                    },

                    DateTimeOffset.Now.AddMinutes(TokenDurationInMinutes));

                response.AccessToken = $"bearer {token}";
                return response;
            }

            return new Response();
        }

        
    }
}