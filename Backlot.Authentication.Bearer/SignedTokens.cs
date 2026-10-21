using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backlot.Authentication.Bearer;

/// <summary>
/// Creates and validates HMAC signed JWTs for this installation.
/// Used for access and refresh tokens (JwtTokenStore) and for the short-lived authorization codes and gateway state.
/// Every token gets a "typ" so one kind of token can never be used as another.
/// </summary>
public sealed class SignedTokens(BearerOptions options)
{
    public const string AccessType = "access";
    public const string RefreshType = "refresh";
    public const string CodeType = "code";
    public const string StateType = "state";

    private readonly JsonWebTokenHandler _handler = new();

    private SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(options.SigningKey));

    public string Create(string type, TimeSpan lifetime, IDictionary<string, object> claims)
    {
        var all = new Dictionary<string, object>(claims)
        {
            ["typ"] = type,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
        };

        var now = DateTime.UtcNow;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            Claims = all,
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256)
        });
    }

    /// <summary>
    /// Returns the claims of a valid token of the given type, or null.
    /// </summary>
    public async Task<ClaimsIdentity?> Validate(string token, string type)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = Key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        });

        if (!result.IsValid) return null;

        return result.ClaimsIdentity.FindFirst("typ")?.Value == type ? result.ClaimsIdentity : null;
    }
}
