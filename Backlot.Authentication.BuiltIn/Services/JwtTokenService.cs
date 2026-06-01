using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Backlot.Authentication.BuiltIn.Services;

/// <summary>
/// Token Service does manage the creation and validation of tokens based on the Jwt standard.
/// We add some custom claims to the token to make it more secure.                             
/// </summary>
public class JwtTokenService
{
    private readonly string _secret;

        public JwtTokenService(string secret)
        {
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        }

        /// <summary>
        /// Always creates a new token with a new jti.
        /// </summary>
        /// <param name="claimsToAdd"></param>
        /// <param name="utcExperationDateTime">Expiration date is in the future, depending on the use case this can be hours, days or months.</param>
        /// <returns></returns>
        public (string id, string token) CreateRefreshToken(IDictionary<string, string> claimsToAdd,
            DateTimeOffset utcExperationDateTime)
        {
            var jti = Guid.NewGuid().ToString("N"); // long live tokens do get a *new* jti, ALWAYS.
            // this to avoid sliding expiration on the same jti,
            // it also gives the opportunity to revoke a token using the jti only
            // no no sensitive information is stored in the database.

            if (!claimsToAdd.TryAdd(JwtRegisteredClaimNames.Jti, jti))
                claimsToAdd[JwtRegisteredClaimNames.Jti] = jti;

            if (!claimsToAdd.TryAdd(ClaimNames.TokenType, TokenTypes.Refresh))
                claimsToAdd[ClaimNames.TokenType] = TokenTypes.Refresh;

            var token = Create(claimsToAdd, utcExperationDateTime);

            return (jti, token);
        }

        /// <summary>
        /// Creates a short live access token with the same jti as the parent refresh token.
        /// </summary>
        /// <param name="claimsToAdd">Requires  and jti are part of the claims</param>
        /// <param name="utcExperationDateTime">Ensure expiration is in the very near future.</param>
        /// <returns></returns> 
        public string CreateAccessToken(IDictionary<string, string> claimsToAdd,
            DateTimeOffset utcExperationDateTime)
        {
            // short live tokens do use an existing jti
            if (!claimsToAdd.ContainsKey(JwtRegisteredClaimNames.Jti))
            {
                throw new ArgumentException(
                    $"To create a valid access token jti and  must be part of the claims.");
            }

            if (!claimsToAdd.TryAdd(ClaimNames.TokenType, TokenTypes.Access))
                claimsToAdd[ClaimNames.TokenType] = TokenTypes.Access; // set token type to ACCESS

            var token = Create(claimsToAdd, utcExperationDateTime);

            return token;
        }

        /// <summary>
        /// Creates a jwt token with the given claims and expiration date.
        /// </summary>
        /// <param name="claimsToAdd"></param>
        /// <param name="utcExperationDateTime"></param>
        /// <returns></returns>
        private string Create(IDictionary<string, string> claimsToAdd, DateTimeOffset utcExperationDateTime)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsToAdd.Where(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value)).Select(x => new Claim(x.Key, x.Value))),
                Expires = utcExperationDateTime.UtcDateTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };
    
            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool IsValid((SecurityToken? securityToken, ClaimsPrincipal? principal) token)
        {
            try
            {
                if (token.securityToken?.ValidTo == null || DateTime.UtcNow > token.securityToken.ValidTo)
                {
                    return false;
                }
                
                var tokenType = token.principal?.Claims.FirstOrDefault(x => x.Type == ClaimNames.TokenType)?.Value;

                if (tokenType == TokenTypes.Refresh) return true; // refresh tokens are valid when they are not expired.


                if (tokenType == TokenTypes.Access) // access tokens are valid if they are not expired and have a  and jti claim
                {
                    var jti = token.principal?.Claims
                        .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (string.IsNullOrEmpty(jti))
                        return false; // jti and  are required                                 

                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public IDictionary<string, string> GetClaims((SecurityToken? securityToken, ClaimsPrincipal? principal) token)
        {
            var claims = (token.securityToken as JwtSecurityToken)?.Claims;
            
            return claims == null ?
                new Dictionary<string, string>() : 
                claims.ToDictionary(x => x.Type, x => x.Value);
        }

        public (SecurityToken? securityToken, ClaimsPrincipal? principal) GetTokenAndPrincipals(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return (null, null);
            }

            var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = mySecurityKey
                }, out var validatedToken);

                return (validatedToken, principal);
            }
            catch
            {
                return (null, null);
            }
        }

        public (string Id, string TokenType, string Username, string Groups) ClaimNames => (
                JwtRegisteredClaimNames.Jti, 
                "token_type", 
                JwtRegisteredClaimNames.Sub, 
                "groups");
}



// documentation: -- only one implementation therefor do not need an interface, only used internally.

/*
namespace Backlot.Authentication.BuiltIn;

public interface ITokenService
{
    /// <summary>
    /// Generates a long term token. These can be used to create new refresh tokens or to create access tokens for authentication.
    /// A refresh token does NOT authenticate the user.
    /// </summary>
    /// <param name="claimsToAdd"></param>
    /// <param name="utcExperationDateTime">Expiration date is in the future, depending on the use case this can be hours, days or months.</param>
    /// <returns>The stringified version of the token + the unique id</returns>
    (string id, string token) CreateRefreshToken(IDictionary<string, string> claimsToAdd,
        DateTimeOffset utcExperationDateTime);

    /// <summary>
    /// Generates short live tokens. These tokens are valid for authentication.
    /// Can be used by IUserContext, ideally this token is short lived.
    /// </summary>
    /// <param name="claimsToAdd">Requires a registered jti is part of the claims</param>
    /// <param name="utcExperationDateTime">Ensure expiration is in the very near future.</param>
    /// <returns>The stringified version of the token.</returns>
    string CreateAccessToken(IDictionary<string, string> claimsToAdd, DateTimeOffset utcExperationDateTime);

    /// <summary>
    /// Check if this is a valid "access" or "refresh" token.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    bool IsValid((SecurityToken? securityToken, ClaimsPrincipal? principal) token);
    
    /// <summary>
    /// Get the claims from the token.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    IDictionary<string, string> GetClaims((SecurityToken? securityToken, ClaimsPrincipal? principal) token);

    /// <summary>
    /// Get the token and principal from a stringified token.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    (SecurityToken? securityToken, ClaimsPrincipal? principal) GetTokenAndPrincipals(string token);

    (string Id, string TokenType, string Username, string Groups) ClaimNames { get; }
    
}*/