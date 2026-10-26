using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;

namespace Backlot.Defaults.Scenarios.Authentication;

/// <summary>
/// Returns the authentication scheme of this installation, so clients know how to log in:
/// - "Basic": send base64(username:password) with every request.
/// - "Bearer": Backlot issues tokens (authorization code flow with PKCE), either internally or as a gateway to an external identity provider.
/// Open for anonymous users.
/// </summary>
[Scenario(typeof(AuthInfo), access: [Access.Open])]
public class AuthInfo : DirectorScenario<AuthInfo, object>
{
    public AuthInfo(IDirector role) : base(role)
    {
    }

    protected override object Exec()
    {
        var scheme = UserContext.Current.AuthScheme;

        if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return new
            {
                Scheme = UserContext.Current.AuthScheme
                //??? LoginUrl = "https://localhost:7221/login" // TODO; make configurable. 
            };
        }

        return new
        {
            Scheme = UserContext.Current.AuthScheme
        };
    }
}
