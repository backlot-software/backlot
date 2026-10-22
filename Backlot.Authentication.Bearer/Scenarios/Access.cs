using System.ComponentModel.DataAnnotations;
using Backlot.Authentication.Bearer.Roles;
using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Authentication.Bearer.Scenarios;

/// <summary>
/// Exchanges an authorization code (with PKCE code verifier) for a new token set.
/// </summary>
[Scenario(typeof(Access), access: [Core.Security.Access.Open])]
public class Access(IAuth role, ITokenStore tokenStore)
    : Scenario<Access, IAuth, TokenSet>(role)
{
    protected override bool PersistAndRelate => false;

    public override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Role.Code))
            ValidationResults.Add(new ValidationResult("Code is required.", [nameof(IAuth.Code)]));
        if (string.IsNullOrWhiteSpace(Role.CodeVerifier))
            ValidationResults.Add(new ValidationResult("CodeVerifier is required.", [nameof(IAuth.CodeVerifier)]));
        if (string.IsNullOrWhiteSpace(Role.RedirectUrl))
            ValidationResults.Add(new ValidationResult("RedirectUrl is required.", [nameof(IAuth.RedirectUrl)]));

        return ValidationResults.Count == 0;
    }

    protected override async Task<TokenSet> ExecAsync()
    {
        var user = await new AuthorizationCodes(BearerOptions.FromConfiguration())
                       .Redeem(Role.Code, Role.CodeVerifier, Role.RedirectUrl)
                   ?? throw new UnauthorizedAccessException("Invalid authorization code.");

        return await tokenStore.Issue(user.userName, user.groups);
    }
}
