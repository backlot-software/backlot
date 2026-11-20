using System.Security.Cryptography;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.Scenarios;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.UseCases.RolePersistence;

/// <summary>
/// As a Backlot developer,
/// I want to persist and retrieve self roles,
/// so that their type is preserved without unnecessary proxies.
/// </summary>
public class SelfRoleRoundTrip
{
    [SetUp]
    public void Setup()
    {
        Defaults.Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b),
                configureContainer: b =>
                {
                    b.RegisterType<MemoryPersistedRoleRepository>()
                        .As<IPersistedRoleRepository>();

                    b.RegisterType<MemoryRelationRepository>()
                        .As<IRelationRepository>();
                    
                    b.Register(_ => new ChecksumBuilder(MD5.Create().ComputeHash))
                        .As<IChecksumBuilder>();
                }));
        // Ensure the repository starts empty.
        ServiceLocator.Get<IPersistedRoleRepository>().FlushDb();
    }
    
    [Test]
    public async Task GivenSelfRole_WhenPresentedPersistedAndRetrievedFromRepo_ThenRoleIsNotProxiedAndTypeIsPreserved()
    {
        //A self object is an object implementing a role interface itself. 
        
        // Arrange
        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        // Act
        var presented = self.Presents<IFormula>();
        await Dummy.Play(presented);
        repo.TryGet<IFormula>(self.Uid, out var dbEntity);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(presented, Is.Not.InstanceOf<IProxiedRole>(), $"The self instance did presents as a proxied role, which is not allowed / needed.");
            Assert.That(presented, Is.TypeOf<FormulaSelf>(), $"The self instance did not presents it self a type of the same instance");
            Assert.That(dbEntity, Is.TypeOf<FormulaSelf>(), $"The self instance did not presents it self a type of the same instance when loaded from the dbrepo");
        });
    }
}