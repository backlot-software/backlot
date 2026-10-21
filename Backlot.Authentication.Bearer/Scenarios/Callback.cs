using System.Security.Claims;
using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Http;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Gateway login: exchanges the code of the external identity provider, maps the user
/// and returns the location to redirect the browser to, with a Backlot authorization code.
/// </summary>
[Scenario(typeof(Callback), access: [Access.Open])]
public class Callback(ICallbackRequest role)
    : Scenario<Callback, ICallbackRequest, object>(role)
{
    // composition
    /// <summary>
    /// Maps the claims of the identity provider to a Backlot user. By default GatewayUserNameClaim and GatewayGroupsClaim are used.
    /// Return success false to deny the user.
    /// </summary>
    public Func<ClaimsIdentity, (string userName, string[] groups), Task<(bool success, string userName, string[] groups)>> MapUser { get; set; } =
        (_, mapped) => Task.FromResult((!string.IsNullOrWhiteSpace(mapped.userName), mapped.userName, mapped.groups));

    protected override bool PersistAndRelate => false;

    protected override async Task<object> ExecAsync()
    {
        var options = BearerOptions.FromConfiguration();
        if (!options.IsGateway)
            throw new BadRequestException("No external identity provider configured.");

        var codes = new AuthorizationCodes(options);
        var request = await codes.ReadGatewayState(Role.State)
                      ?? throw new BadRequestException("Invalid or expired state.");

        var gateway = new OidcGateway(options);
        var identity = await gateway.Exchange(Role.Code)
                       ?? throw new UnauthorizedAccessException("The identity provider did not accept the login.");

        var user = await MapUser(identity, gateway.MapUser(identity));
        if (!user.success)
            throw new UnauthorizedAccessException("User is not allowed.");

        var code = codes.IssueCode(user.userName, user.groups, request.redirectUri, request.codeChallenge);
        return new { Location = AuthorizationCodes.RedirectLocation(request.redirectUri, code, request.state) };
    }
}
