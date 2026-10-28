namespace Backlot.Studio.Core.Models.Response;

/// <summary>Result of the AuthInfo scenario.</summary>
public class AuthInfoBody
{
    public string Scheme { get; set; } = string.Empty;
}

/// <summary>Result of the gateway Authorize and Callback scenarios: where to send the browser next.</summary>
public class LocationBody
{
    public string? Location { get; set; }
}

/// <summary>Result of the gateway Access and Refresh scenarios.</summary>
public class TokenSetBody
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
