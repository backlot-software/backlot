using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;

#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor.

namespace Backlot.Authentication.Bearer;

/// <summary>
/// Settings for Bearer authentication. Backlot is a gateway to the external identity provider and issues its own tokens.
/// </summary>
public class Settings
{
    /// <summary>Issuer (iss) of the tokens issued by this installation, e.g. https://api.example.com</summary>
    [Configurable] public string Issuer { get; set; }

    /// <summary>Audience (aud) of the tokens issued by this installation.</summary>
    [Configurable] public string Audience { get; set; }

    /// <summary>Secret used to sign tokens, codes and state (HMAC SHA256). At least 32 characters.</summary>
    [Configurable] public string SigningKey { get; set; }

    /// <summary>Lifetime of access tokens in minutes (default 15).</summary>
    [Configurable] public string AccessTokenLifetimeMinutes { get; set; }

    /// <summary>Lifetime of refresh tokens in days (default 14).</summary>
    [Configurable] public string RefreshTokenLifetimeDays { get; set; }

    /// <summary>Comma separated list of redirect uris clients are allowed to use (exact match).</summary>
    [Configurable] public string AllowedRedirectUris { get; set; }

    /// <summary>Authority of the external identity provider, e.g. https://tenant.eu.auth0.com/ (required for Authorize and Callback).</summary>
    [Configurable] public string GatewayAuthority { get; set; }

    [Configurable] public string GatewayClientId { get; set; }

    [Configurable] public string GatewayClientSecret { get; set; }

    /// <summary>Optional audience parameter sent to the identity provider (Auth0 api identifier).</summary>
    [Configurable] public string GatewayAudience { get; set; }

    /// <summary>Scopes requested at the identity provider (default "openid profile email").</summary>
    [Configurable] public string GatewayScopes { get; set; }

    /// <summary>
    /// Absolute uri of the LoginPage scenario, registered as callback at the identity provider,
    /// e.g. https://api.example.com/api/role/director/loginpage
    /// </summary>
    [Configurable] public string GatewayCallbackUri { get; set; }

    /// <summary>Claim used as username (default "email").</summary>
    [Configurable] public string GatewayUserNameClaim { get; set; }

    /// <summary>Claim holding the groups (default "groups").</summary>
    [Configurable] public string GatewayGroupsClaim { get; set; }
}

/// <summary>
/// Typed and defaulted view on <see cref="Settings"/>.
/// </summary>
public sealed class BearerOptions
{
    public string Issuer { get; init; } = "backlot";
    public string Audience { get; init; } = "backlot";
    public string SigningKey { get; init; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);
    public string[] AllowedRedirectUris { get; init; } = [];

    public string GatewayAuthority { get; init; } = string.Empty;
    public string GatewayClientId { get; init; } = string.Empty;
    public string GatewayClientSecret { get; init; } = string.Empty;
    public string GatewayAudience { get; init; } = string.Empty;
    public string GatewayScopes { get; init; } = "openid profile email";
    public string GatewayCallbackUri { get; init; } = string.Empty;
    public string GatewayUserNameClaim { get; init; } = "email";
    public string GatewayGroupsClaim { get; init; } = "groups";

    public static BearerOptions FromConfiguration()
    {
        var configuration = ServiceLocator.Get<IConfigurationManager>();
        string Get(System.Linq.Expressions.Expression<Func<Settings, string>> setting) => configuration.Get(setting).Trim();
        string Or(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        var defaults = new BearerOptions();

        var options = new BearerOptions
        {
            Issuer = Or(Get(s => s.Issuer), defaults.Issuer),
            Audience = Or(Get(s => s.Audience), defaults.Audience),
            SigningKey = Get(s => s.SigningKey),
            AccessTokenLifetime = double.TryParse(Get(s => s.AccessTokenLifetimeMinutes), out var minutes)
                ? TimeSpan.FromMinutes(minutes) : defaults.AccessTokenLifetime,
            RefreshTokenLifetime = double.TryParse(Get(s => s.RefreshTokenLifetimeDays), out var days)
                ? TimeSpan.FromDays(days) : defaults.RefreshTokenLifetime,
            AllowedRedirectUris = Get(s => s.AllowedRedirectUris)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            GatewayAuthority = Get(s => s.GatewayAuthority),
            GatewayClientId = Get(s => s.GatewayClientId),
            GatewayClientSecret = Get(s => s.GatewayClientSecret),
            GatewayAudience = Get(s => s.GatewayAudience),
            GatewayScopes = Or(Get(s => s.GatewayScopes), defaults.GatewayScopes),
            GatewayCallbackUri = Get(s => s.GatewayCallbackUri),
            GatewayUserNameClaim = Or(Get(s => s.GatewayUserNameClaim), defaults.GatewayUserNameClaim),
            GatewayGroupsClaim = Or(Get(s => s.GatewayGroupsClaim), defaults.GatewayGroupsClaim)
        };

        if (options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                $"{typeof(Settings).FullName}.{nameof(Settings.SigningKey)} must be configured with at least 32 characters.");

        return options;
    }
}
