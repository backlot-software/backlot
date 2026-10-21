using Backlot.Core.Services;

namespace Backlot.Authentication.Bearer;

/// <summary>
/// Default stateless <see cref="ITokenStore"/>: access and refresh tokens are signed JWTs.
/// Nothing is stored, so revoking only happens by expiry. Register your own ITokenStore when revocation is required.
/// </summary>
public class JwtTokenStore : ITokenStore
{
    private BearerOptions? _options;
    protected BearerOptions Options => _options ??= BearerOptions.FromConfiguration();

    private SignedTokens Tokens => new(Options);

    public Task<TokenSet> Issue(string userName, string[] groups)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = userName,
            ["groups"] = groups
        };

        var access = Tokens.Create(SignedTokens.AccessType, Options.AccessTokenLifetime, claims);
        var refresh = Tokens.Create(SignedTokens.RefreshType, Options.RefreshTokenLifetime, claims);

        return Task.FromResult(new TokenSet(access, refresh, (int)Options.AccessTokenLifetime.TotalSeconds));
    }

    public async Task<TokenUser?> ValidateAccess(string accessToken)
    {
        var identity = await Tokens.Validate(accessToken, SignedTokens.AccessType);
        if (identity == null) return null;

        var userName = identity.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userName)) return null;

        return new TokenUser(
            userName,
            identity.FindAll("groups").Select(c => c.Value).ToArray(),
            identity.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value))));
    }

    public async Task<TokenSet?> Refresh(string refreshToken)
    {
        var identity = await Tokens.Validate(refreshToken, SignedTokens.RefreshType);
        var userName = identity?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userName)) return null;

        return await Issue(userName, identity!.FindAll("groups").Select(c => c.Value).ToArray());
    }

    public virtual Task Revoke(string refreshToken) => Task.CompletedTask;
}
