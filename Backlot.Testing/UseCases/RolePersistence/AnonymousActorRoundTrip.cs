using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.Testing.Shared.Scenarios;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.UseCases.RolePersistence;

/// <summary>
/// As a Backlot developer,
/// I want to persist and retrieve an anonymous actor presented as a role,
/// so that its properties remain available after retrieval.
/// </summary>
public class AnonymousActorRoundTrip
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public async Task GivenAnonymousActor_WhenPersistedAndRetrieved_ThenJsonBackedRoleRetainsProperties()
    {
        // Arrange
        var initial = new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();

        // Act
        var role = initial.Presents<IFormula>();
        await Dummy.Play(role);
        repo.TryGet<IFormula>(initial.Uid, out var persisted);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(((IProxiedRole)persisted).Actor, Is.Not.Null);
            Assert.That(persisted, Is.InstanceOf<IProxiedRole>());
            Assert.That(((IProxiedRole)persisted).Actor, Is.InstanceOf<JObject>());
            Assert.That(persisted.Number1, Is.EqualTo(7));
        });
    }
    
}