using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Microsoft.IdentityModel.Tokens;
// We make sure everthing is set within Initialize;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Backlot.Experimental.Authentication.Auth0;

/// <summary>
/// The Implementation of the IUserContext when using Auth0.
/// </summary>
public class Auth0UserContext : IUserContext
{
    private static IConfigurationManager ConfigurationManager => ServiceLocator.Get<IConfigurationManager>();
    protected string Auth0Domain => ConfigurationManager.Get<Settings>(s => s.Auth0Domain);
    protected string Auth0Audience => ConfigurationManager.Get<Settings>(s => s.Auth0Audience);
    protected string UserNameClaim => ConfigurationManager.Get<Settings>(s => s.UserNameClaim);
    
    protected (SecurityToken? securityToken, ClaimsPrincipal? principal) TokenAndPrincipal { get; private set; }

    #region Initialization
    
    public Task Intialize(string token)
    {
        Groups = ["Users"]; //todo: make this dynamic based on claims or a group repository.
        TokenAndPrincipal = GetTokenAndPrincipals(token);
        return Task.CompletedTask;
    }

    public string AuthScheme => "Bearer";

    public string Token
    {
        set => Intialize(value);
    }
    
    #endregion
    
    #region Directly From Token
    
    public string UserName => Claims[UserNameClaim];
    
    private IDictionary<string, string>? _claims;
    
    /// <summary>
    /// The claims defined for this token.
    /// </summary>
    public IDictionary<string, string> Claims
    {
        get
        {
            if (_claims == null) // only GetClaims once per handler.
            {
                _claims = new Dictionary<string, string>();
                if (TokenAndPrincipal.principal != null)
                {
                    foreach (var claim in TokenAndPrincipal.principal.Claims)
                    {
                        if (!_claims.ContainsKey(claim.Type))
                            _claims.Add(claim.Type, claim.Value);
                    }
                }
                
                return _claims;
            }

            return _claims;
        }
    }
    
    #endregion
    
    public string[] Groups { private set; get; } // set by initialize
    public bool IsInGroup(string groupname)
    {
        return Groups
            .Any(r => r.Equals(groupname, StringComparison.CurrentCultureIgnoreCase));
    }

    private bool? _isAuthenticated;

    public bool IsAuthenticated
    {
        get
        {
            if(_isAuthenticated.HasValue)
                return _isAuthenticated.Value;
            
            // Token and principal must be non-null
            if (TokenAndPrincipal.securityToken == null || TokenAndPrincipal.principal == null) return false;

            // Validate "iss" claim matches your Auth0 domain
            var issuer = TokenAndPrincipal.principal.FindFirst("iss")?.Value;
            if (string.IsNullOrEmpty(issuer) || !issuer.Contains(Auth0Domain))
            {
                _isAuthenticated = false;
                return _isAuthenticated.Value;
            }

            // Validate audience if needed
            // var audience = token.principal.FindFirst("aud")?.Value;
            // if (audience != "YOUR_AUTH0_API_AUDIENCE")
            //    return false;

            _isAuthenticated = true;
            return _isAuthenticated.Value;
        }
    }

    protected virtual (SecurityToken? securityToken, ClaimsPrincipal? principal) GetTokenAndPrincipals(string token)
    {
        
        var handler = new JwtSecurityTokenHandler();
        SecurityToken? securityToken = null;
        ClaimsPrincipal? principal = null;

        // VALIDATION PARAMETERS
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Auth0Domain,      // Replace with your Auth0 domain
            ValidAudience = Auth0Audience,      // Replace with your API audience
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_,_,_,_) =>
            {
                // This will be called if the key for the 'kid' is not cached
                // and fetch the keys from Auth0's JWKS endpoint
                var client = new HttpClient();
                var jwks = client.GetStringAsync($"{Auth0Domain}.well-known/jwks.json").Result;
                var keys = new JsonWebKeySet(jwks);
                return keys.Keys;
            }
        };

        try
        {
            principal = handler.ValidateToken(token, validationParameters, out securityToken);
        }
        catch
        {
            return (null, null);
        }

        return (securityToken, principal);
    }
   
}