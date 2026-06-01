using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Backlot.Authentication.BuiltIn.Services;

/// <summary>
/// INTERNAL: Base for handeling tokens using the ITokenService.
/// </summary>
public class TokenHandlerBase
{
    protected readonly JwtTokenService TokenService;
    
    internal TokenHandlerBase(JwtTokenService tokenService, string? token)
    {
        TokenService = tokenService;
        
        if(token != null) // turn the token into a workable object.
            TokenAndPrincipal = tokenService.GetTokenAndPrincipals(token);
    }
    
    protected (SecurityToken? securityToken, ClaimsPrincipal? principal) TokenAndPrincipal { get; set; } 

    /// <summary>
    /// The username related to this token.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public string UserName
    {
        get
        {
            try
            {
                return Claims[TokenService.ClaimNames.Username];
            }
            catch (KeyNotFoundException)
            {
                throw new UnauthorizedAccessException("Access denied. Invalid token or user.");
            }
        }
    }

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
                _claims = TokenService.GetClaims(TokenAndPrincipal).ToDictionary(k => k.Key, v => v.Value);
            }

            return _claims;
        }
    }

    private bool? _isValid;

    /// <summary>
    /// Is this a valid token. based on token type requirements and expiration.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (_isValid == null) // only check once per handler.
            {
                _isValid = TokenService.IsValid(TokenAndPrincipal);
            }

            return _isValid.Value;
        }
    }
}