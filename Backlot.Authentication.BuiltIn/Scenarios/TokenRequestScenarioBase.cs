using System;
using Backlot.Authentication.BuiltIn.Roles;
using Backlot.Authentication.BuiltIn.Services;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;

namespace Backlot.Authentication.BuiltIn.Scenarios
{
    /// <summary>
    /// INTERNAL: Base implementation of creating tokens, used for both access and refresh tokens.
    /// </summary>
	public abstract class TokenRequestScenarioBase
        : Scenario<TokenRequestScenarioBase, ITokenRequest, TokenRequestScenarioBase.Response>
    {
        
        private readonly IConfigurationManager _configurationManager;
        
        protected readonly ITokenRepository TokenRepository;
        protected readonly JwtTokenService TokenService;
        protected TokenHandlerBase RefreshToken;

        internal TokenRequestScenarioBase(ITokenRequest role, JwtTokenService tokenService, ITokenRepository tokenRepository, IConfigurationManager configurationManager) : base (role)
        {
            _configurationManager = configurationManager;
            TokenService = tokenService;
            TokenRepository = tokenRepository;
            RefreshToken = new TokenHandlerBase(tokenService, role.RefreshToken);
        }
        
        public abstract int TokenDurationInMinutes { get; set; }

        /// <summary>
        /// When a new refresh token is created, the jti of the old refresh token is revoked.
        /// This is managed through an authentication database and does come with a small performance hit.
        /// It on the other hand ensures a much higher level of security.
        /// </summary>
        protected bool ManageTokenRevocation => Convert.ToBoolean(_configurationManager.Get<Settings>(s => s.ManageTokenRevocation));

        public override bool Validate() // validate the TokenRequest
        {
            if (base.Validate())
            {
                // the RefreshToken needs to be checked
                if (!RefreshToken.IsValid)
                    return false;
                
                if(RefreshToken.Claims[TokenService.ClaimNames.TokenType] != TokenTypes.Refresh)
                    return false;

                if (ManageTokenRevocation) // in this case we need to check database for jti revocation
                {
                    if (TokenRepository.IsRevoked(RefreshToken.Claims[TokenService.ClaimNames.Id]))
                        return false;
                }

                return true;
            }
            
            return false;
        }
        
        public class Response : ITokenRequest
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
        }
    }
}