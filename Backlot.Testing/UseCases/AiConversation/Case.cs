using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Exceptions;
using Backlot.Http;
using Backlot.Services.AI.Roles;
using Backlot.Services.AI.Scenarios;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Core;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.AiConversation;

/// <summary>
/// The Create scenario (Backlot.Services.AI) turns one chat message into the next question plus the
/// running lists of role and scenario names. The outbound Mistral call is the only thing stubbed —
/// everything else runs through the real ScenarioBuilder, so the DI contract (role first, then
/// IHttpClientFactory and IConfigurationManager from the container) is exercised too.
/// </summary>
public class Case
{
    private const string ConfigPrefix = "Backlot.Services.AI.Settings";

    private static void SetupWith(HttpMessageHandler handler, string apiKey = "test-key")
    {
        Initialize.Setup(buildDirector: builder =>
        {
            builder.RegisterInstance<IHttpClientFactory>(new StubHttpClientFactory(handler));

            var config = new ConfigStub(new Dictionary<string, string>
            {
                { $"{ConfigPrefix}.ApiKey", apiKey },
                { $"{ConfigPrefix}.Endpoint", "https://stub.invalid/v1/chat/completions" },
                { $"{ConfigPrefix}.Model", "stub-model" }
            });

            return new Director(new LocalDiskStorage(), config, builder);
        });
    }

    private static IConversation Conversation(string message, string[] roles = null, string[] scenarios = null)
    {
        var role = Acting.New<IConversation>();
        role.Message = message;
        role.Roles = roles ?? [];
        role.Scenarios = scenarios ?? [];
        return role;
    }

    [Test]
    public async Task Play_ModelAnswersWithTheAgreedShape_MessageAndNamesAreReturned()
    {
        #region ARRANGE

        var handler = StubHttpMessageHandler.Completing(
            """{"message":"Who can place an order?","roles":["IProduct","IOrder"],"scenarios":["Checkout"]}""");
        SetupWith(handler);

        #endregion

        #region ACT

        var result = await Create.Play(Conversation("I want a webshop"));

        #endregion

        #region ASSERT

        Assert.That(result.Message, Is.EqualTo("Who can place an order?"));
        Assert.That(result.Roles, Is.EquivalentTo(new[] { "IProduct", "IOrder" }));
        Assert.That(result.Scenarios, Is.EquivalentTo(new[] { "Checkout" }));

        // The turn the model sees is the message plus the state agreed so far — there is no transcript.
        Assert.That(handler.SentBodies, Has.Count.EqualTo(1));
        Assert.That(handler.SentBodies[0], Does.Contain("I want a webshop"));

        #endregion
    }

    [Test]
    public async Task Play_ModelReturnsAShorterList_PreviouslyAgreedNamesSurvive()
    {
        #region ARRANGE

        // The model was told to return the complete lists but returns only part of them. A turn must
        // never be able to drop names the conversation already agreed on.
        var handler = StubHttpMessageHandler.Completing(
            """{"message":"And then?","roles":["IOrder"],"scenarios":[]}""");
        SetupWith(handler);

        #endregion

        #region ACT

        var result = await Create.Play(Conversation("continue", ["IProduct"], ["Checkout"]));

        #endregion

        #region ASSERT

        Assert.That(result.Roles, Is.EquivalentTo(new[] { "IProduct", "IOrder" }));
        Assert.That(result.Scenarios, Is.EquivalentTo(new[] { "Checkout" }));

        #endregion
    }

    [Test]
    public void Play_ModelAnswersWithSomethingElseThanTheAgreedShape_BadRequestException()
    {
        #region ARRANGE

        var handler = StubHttpMessageHandler.Completing("I am afraid I cannot do that.");
        SetupWith(handler);

        #endregion

        #region ACT + ASSERT

        // A parse failure has to reach the operator as a 400 with a readable reason, not as a raw
        // JsonException turning into an anonymous 500.
        Assert.ThatAsync(() => Create.Play(Conversation("I want a webshop")),
            Throws.TypeOf<BadRequestException>());

        #endregion
    }

    [Test]
    public void Play_ApiKeyIsNotConfigured_BadRequestExceptionAndNoCallIsMade()
    {
        #region ARRANGE

        var handler = StubHttpMessageHandler.Completing("""{"message":"hi","roles":[],"scenarios":[]}""");
        SetupWith(handler, apiKey: string.Empty);
        System.Environment.SetEnvironmentVariable("MISTRAL_API_KEY", null);

        #endregion

        #region ACT + ASSERT

        Assert.ThatAsync(() => Create.Play(Conversation("I want a webshop")),
            Throws.TypeOf<BadRequestException>());
        Assert.That(handler.SentBodies, Is.Empty);

        #endregion
    }

    [Test]
    public void Play_UpstreamAnswersWithAnError_BadRequestException()
    {
        #region ARRANGE

        var handler = new StubHttpMessageHandler("""{"error":"nope"}""", HttpStatusCode.TooManyRequests);
        SetupWith(handler);

        #endregion

        #region ACT + ASSERT

        Assert.ThatAsync(() => Create.Play(Conversation("I want a webshop")),
            Throws.TypeOf<BadRequestException>());

        #endregion
    }

    [Test]
    public void Play_MessageIsBlank_ValidationException()
    {
        #region ARRANGE

        var handler = StubHttpMessageHandler.Completing("""{"message":"hi","roles":[],"scenarios":[]}""");
        SetupWith(handler);

        #endregion

        #region ACT + ASSERT

        Assert.ThatAsync(() => Create.Play(Conversation("   ")),
            Throws.TypeOf<ValidationException>());
        Assert.That(handler.SentBodies, Is.Empty);

        #endregion
    }
}
