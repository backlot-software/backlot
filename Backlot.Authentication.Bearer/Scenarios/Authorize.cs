using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Starts the authorization code flow, called by the LoginPage.
/// Returns the location of the external identity provider (gateway), or no location when the login is handled internally.
/// </summary>
[Scenario(typeof(Authorize), access: [Access.Open])]
public class Authorize(IAuthorizeRequest role)
    : Scenario<Authorize, IAuthorizeRequest, object>(role)
{
    protected override bool PersistAndRelate => false;

    protected override async Task<object> ExecAsync()
    {
        var options = BearerOptions.FromConfiguration();
        var codes = new AuthorizationCodes(options);

        codes.ValidateRequest(Role.RedirectUri, Role.CodeChallenge);

        if (!options.IsGateway)
        {
            return new { Location = (string?)null };
        }

        var gatewayState = codes.CreateGatewayState(Role.RedirectUri, Role.State, Role.CodeChallenge);
        return new { Location = (string?)await new OidcGateway(options).AuthorizeUrl(gatewayState) };
    }
}
