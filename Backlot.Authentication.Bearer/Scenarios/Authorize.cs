using System.ComponentModel.DataAnnotations;
using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Starts the authorization code flow, called by the LoginPage.
/// Returns the location of the external identity provider to send the browser to.
/// </summary>
[Scenario(typeof(Authorize), access: [Core.Security.Access.Open])]
public class Authorize(IAuth role)
    : Scenario<Authorize, IAuth, object>(role)
{
    protected override bool PersistAndRelate => false;

    public override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Role.RedirectUrl))
            ValidationResults.Add(new ValidationResult("RedirectUrl is required.", [nameof(IAuth.RedirectUrl)]));
        if (string.IsNullOrWhiteSpace(Role.State))
            ValidationResults.Add(new ValidationResult("State is required.", [nameof(IAuth.State)]));
        if (string.IsNullOrWhiteSpace(Role.CodeChallenge))
            ValidationResults.Add(new ValidationResult("CodeChallenge is required.", [nameof(IAuth.CodeChallenge)]));

        return ValidationResults.Count == 0;
    }

    protected override async Task<object> ExecAsync()
    {
        var options = BearerOptions.FromConfiguration();
        var codes = new AuthorizationCodes(options);

        codes.ValidateRequest(Role.RedirectUrl, Role.CodeChallenge);

        var gatewayState = codes.CreateGatewayState(Role.RedirectUrl, Role.State, Role.CodeChallenge);
        return new { Location = await new OidcGateway(options).AuthorizeUrl(gatewayState) };
    }
}
