using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.Scenarios;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.Core.Loader;

public class GetRoleName
{
    # region Backlot Test Environment Setup
    
    [SetUp]
    public void Setup()
    {
        // This fixture contains tests needing different backlot environment configurations.
        // These setups are configured below and called in the tests //Arrange
    }
    
    private static void DefaultSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot());
    }

    private static void PermissionAndRepositorySetup()
    {
        Initialize.BuildApp(builder =>
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
    
    # endregion
    
    /// <summary>
    /// Verifies the role name is derived from an interface name by removing its leading I.
    /// </summary>
    [Test]
    public void GetRoleName_InterfaceNameStartingWithI_ReturnsInterfaceNameWithoutI()
    {
        DefaultSetup();
        
        // Act
        var result = Backlot.Core.Loader.GetRoleName(typeof(IInterfaceStartingWithI));

        // Assert
        Assert.That(result, Is.EqualTo("InterfaceStartingWithI"));
    }

    /// <summary>
    /// Verifies a typed actor presented as a role resolves to its role name.
    /// </summary>
    [Test]
    public void GetRoleName_WithTypedActor_ReturnsRoleName()
    {
        // Arrange
        DefaultSetup();
        
        var origin = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 30,
            Number2 = 21,
            Operation = "sum"
        };
        var role = origin.Presents<IFormula>();

        // Act
        var result = Backlot.Core.Loader.GetRoleName(role.GetType());

        // Assert
        Assert.That(result, Is.EqualTo("Formula"));
    }

    /// <summary>
    /// Verifies a JSON-backed role and its proxy resolve to the same role name.
    /// </summary>
    [Test]
    public void GetRoleName_WithJsonActor_ReturnsRoleNameAndJProxyName()
    {
        // Arrange
        DefaultSetup();
        
        var origin = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });

        var role = origin.Presents<IFormula>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        // Act
        var resultRole = Backlot.Core.Loader.GetRoleName(role.GetType());
        var resultProxy = Backlot.Core.Loader.GetRoleName(jProxy.GetType());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultRole, Is.EqualTo("Formula"));
            Assert.That(resultProxy, Is.EqualTo("Formula"));
        });
        
    }

    /// <summary>
    /// Verifies requesting a role name for a non-role type throws an argument exception.
    /// </summary>
    [Test]
    public void GetRoleName_NonRoleType_ThrowsArgumentException()
    {
        DefaultSetup();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Backlot.Core.Loader.GetRoleName(typeof(JObject)));
    }

    /// <summary>
    /// Verifies a self role and its presented role resolve to the self role's name.
    /// </summary>
    [Test]
    public void GetRoleName_WithSelfRole_ReturnsPresentedRoleNameAndSelfRoleName()
    {
        // Arrange
        DefaultSetup();
        
        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 52,
            Number2 = 100,
            Operation = "sum"
        };

        var presented = self.Presents<IFormula>();
        
        // Act
        var resultPresented = Backlot.Core.Loader.GetRoleName(presented.GetType());
        var resultSelf = Backlot.Core.Loader.GetRoleName(self.GetType());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultPresented, Is.EqualTo("FormulaSelf"));
            Assert.That(resultSelf, Is.EqualTo("FormulaSelf"));
        });
    }

    /// <summary>
    /// Verifies a persisted self role resolves to its role name when loaded from the repository.
    /// </summary>
    [Test]
    public async Task GetRoleName_WithPersistedRole_ReturnsRoleName()
    {
        // Arrange
        PermissionAndRepositorySetup();
        
        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 82,
            Number2 = 22,
            Operation = "sum"
        };

        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        var presented = self.Presents<IFormula>();
        await Dummy.Play(presented);
        var found = repo.TryGet<IFormula>(self.Uid, out var dbentity);
        Assert.That(found, Is.True, "Test setup failed: role was not found"); // gives clearer failure than a NullReferenceException

        // Act
        var resultDb = dbentity.GetType().GetRoleName();

        // Assert
        Assert.That(resultDb, Is.EqualTo("FormulaSelf"));
    }
}