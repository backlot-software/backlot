using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Authentication.BuiltIn.Roles;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;

namespace Backlot.Authentication.BuiltIn.Scenarios
{
    /// <summary>
    /// Creates refresh tokens with a jti only and optionally add them to the database
    /// </summary>
    [Scenario(typeof(Refresh), access: [Core.Security.Access.Open])] // todo: add rate limiting for scenarios like this
	public class Refresh
        : TokenRequestScenarioBase
    {
        [Configurable] public override int TokenDurationInMinutes { get; set; } = 240;
        
        public Refresh(ITokenRequest role, JwtTokenService tokenService, ITokenRepository tokenRepository, IConfigurationManager configurationManager) : base(role, tokenService, tokenRepository, configurationManager)
        {
        }

        protected override async Task<Response> ExecAsync()
        {
            var ttl = DateTimeOffset.Now.AddMinutes(TokenDurationInMinutes);
            var ctx = TokenService.CreateRefreshToken(new Dictionary<string, string>
                {
                    { TokenService.ClaimNames.Username, RefreshToken.UserName }
                },
                ttl);

            if (ManageTokenRevocation)
            {
                // revoke the old token
                await TokenRepository.RevokeAsync(RefreshToken.Claims[TokenService.ClaimNames.Id]);
                // create a new one.
                await TokenRepository.AddAsync(ctx.id, ttl);
            }
            
            return new Response()
            {
                RefreshToken = ctx.token
            };
        }

        
    }
}