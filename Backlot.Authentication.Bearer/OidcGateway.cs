using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace Backlot.Authentication.Bearer;

/// <summary>
/// Gateway to an external OpenID Connect identity provider (Auth0, Entra ID, Keycloak, ...).
/// The client secret never leaves the server; the provider's tokens are only used to establish who the user is.
/// </summary>
public sealed class OidcGateway(BearerOptions options)
{
    private static readonly HttpClient Http = new();

    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> Configurations = new();

    private Task<OpenIdConnectConfiguration> Discover()
    {
        if (string.IsNullOrWhiteSpace(options.GatewayAuthority))
            throw new InvalidOperationException(
                $"{typeof(Settings).FullName}.{nameof(Settings.GatewayAuthority)} must be configured.");

        var authority = options.GatewayAuthority.TrimEnd('/');
        var manager = Configurations.GetOrAdd(authority, a => new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{a}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(Http)));

        return manager.GetConfigurationAsync(CancellationToken.None);
    }

    public async Task<string> AuthorizeUrl(string gatewayState)
    {
        var configuration = await Discover();

        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = options.GatewayClientId,
            ["redirect_uri"] = options.GatewayCallbackUri,
            ["scope"] = options.GatewayScopes,
            ["state"] = gatewayState
        };
        if (!string.IsNullOrWhiteSpace(options.GatewayAudience))
            query["audience"] = options.GatewayAudience;

        return configuration.AuthorizationEndpoint + "?" +
               string.Join("&", query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
    }

    /// <summary>
    /// Exchanges the provider's code and returns the claims of the validated id_token, or null.
    /// </summary>
    public async Task<ClaimsIdentity?> Exchange(string code)
    {
        var configuration = await Discover();

        using var response = await Http.PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = options.GatewayClientId,
                ["client_secret"] = options.GatewayClientSecret,
                ["code"] = code,
                ["redirect_uri"] = options.GatewayCallbackUri
            }));

        if (!response.IsSuccessStatusCode) return null;

        var idToken = JObject.Parse(await response.Content.ReadAsStringAsync())["id_token"]?.ToString();
        if (string.IsNullOrWhiteSpace(idToken)) return null;

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuer = configuration.Issuer,
            ValidAudience = options.GatewayClientId,
            IssuerSigningKeys = configuration.SigningKeys
        });

        return result.IsValid ? result.ClaimsIdentity : null;
    }

    /// <summary>
    /// Default mapping of provider claims to a Backlot user, based on GatewayUserNameClaim and GatewayGroupsClaim.
    /// </summary>
    public (string userName, string[] groups) MapUser(ClaimsIdentity identity)
    {
        var userName = identity.FindFirst(options.GatewayUserNameClaim)?.Value
                       ?? identity.FindFirst("sub")?.Value
                       ?? string.Empty;

        var groups = identity.FindAll(options.GatewayGroupsClaim)
            .SelectMany(c => c.Value.StartsWith('[')
                ? JArray.Parse(c.Value).Select(v => v.ToString())
                : [c.Value])
            .ToArray();

        return (userName, groups);
    }
}
