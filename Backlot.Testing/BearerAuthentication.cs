using System;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Backlot.Authentication.Bearer;
using Backlot.Core.Services;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NUnit.Framework;

namespace Backlot.Testing;

/// <summary>
/// Tests for the stateless tokens, authorization codes and PKCE checks of Backlot.Authentication.Bearer
/// </summary>
public class BearerAuthentication
{
    private const string RedirectUri = "https://app.example.com/callback";
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    private static readonly BearerOptions Options = new()
    {
        SigningKey = "a-very-secret-signing-key-of-32-chars!",
        AllowedRedirectUris = [RedirectUri]
    };

    private static string Challenge(string verifier) =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static IConfigurationManager Configuration()
    {
        var configuration = Substitute.For<IConfigurationManager>();
        configuration.Get(Arg.Any<Expression<Func<Settings, string>>>(), Arg.Any<string>())
            .Returns(ci => ((MemberExpression)ci.Arg<Expression<Func<Settings, string>>>().Body).Member.Name switch
            {
                nameof(Settings.SigningKey) => Options.SigningKey,
                nameof(Settings.AllowedRedirectUris) => RedirectUri,
                _ => string.Empty
            });
        return configuration;
    }

    [Test]
    public async Task Redeem_ValidCodeAndVerifier_ReturnsUser()
    {
        var codes = new AuthorizationCodes(Options);
        var code = codes.IssueCode("jane", ["Admin"], RedirectUri, Challenge(Verifier));

        var user = await codes.Redeem(code, Verifier, RedirectUri);

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Value.userName, Is.EqualTo("jane"));
        Assert.That(user.Value.groups, Is.EqualTo(new[] { "Admin" }));
    }

    [Test]
    public async Task Redeem_WrongVerifierOrRedirectUri_ReturnsNull()
    {
        var codes = new AuthorizationCodes(Options);
        var code = codes.IssueCode("jane", [], RedirectUri, Challenge(Verifier));

        Assert.That(await codes.Redeem(code, Verifier + "x", RedirectUri), Is.Null);
        Assert.That(await codes.Redeem(code, Verifier, "https://evil.example.com/callback"), Is.Null);
    }

    [Test]
    public void ValidateRequest_UnknownRedirectUri_Throws()
    {
        var codes = new AuthorizationCodes(Options);

        Assert.Throws<Backlot.Http.BadRequestException>(() =>
            codes.ValidateRequest("https://evil.example.com/callback", Challenge(Verifier)));
        Assert.DoesNotThrow(() => codes.ValidateRequest(RedirectUri, Challenge(Verifier)));
    }

    [Test]
    public async Task JwtTokenStore_IssueValidateRefresh_RoundTrips()
    {
        var store = new JwtTokenStore(Configuration());

        var tokens = await store.Issue("jane", ["Admin", "Editors"]);
        var user = await store.ValidateAccess(tokens.AccessToken);
        var refreshed = await store.Refresh(tokens.RefreshToken);

        Assert.That(user?.UserName, Is.EqualTo("jane"));
        Assert.That(user?.Groups, Is.EquivalentTo(new[] { "Admin", "Editors" }));
        Assert.That(refreshed, Is.Not.Null);
    }

    [Test]
    public async Task JwtTokenStore_TokenTypesCannotBeMixed()
    {
        var store = new JwtTokenStore(Configuration());
        var tokens = await store.Issue("jane", []);

        Assert.That(await store.ValidateAccess(tokens.RefreshToken), Is.Null);
        Assert.That(await store.Refresh(tokens.AccessToken), Is.Null);
        Assert.That(await store.ValidateAccess("not-a-token"), Is.Null);
    }

    [Test]
    public async Task BearerUserContext_InvalidToken_IsNotAuthenticated()
    {
        var store = new JwtTokenStore(Configuration());
        var tokens = await store.Issue("jane", ["Admin"]);

        var valid = new BearerUserContext(store);
        await valid.Intialize(tokens.AccessToken);
        var invalid = new BearerUserContext(store);
        await invalid.Intialize("garbage");

        Assert.That(valid.IsAuthenticated, Is.True);
        Assert.That(valid.IsInGroup("admin"), Is.True);
        Assert.That(invalid.IsAuthenticated, Is.False);
    }
}
