using System;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Testing.Shared.Scenarios;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.Core.Loader;

public class GetRoleName
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void GetRoleName_InterfaceNameStartingWithI_ReturnsInterfaceNameWithoutI()
    {
        // Act
        var result = Backlot.Core.Loader.GetRoleName(typeof(IInterfaceStartingWithI));

        // Assert
        Assert.That(result, Is.EqualTo("InterfaceStartingWithI"));
    }

    [Test]
    public void GetRoleName_WithTypedActor_ReturnsRoleName()
    {
        // Arrange
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

    [Test]
    public void GetRoleName_WithJsonActor_ReturnsRoleNameAndJProxyName()
    {
        // Arrange
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

    [Test]
    public void GetRoleName_NonRoleType_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Backlot.Core.Loader.GetRoleName(typeof(JObject)));
    }

    [Test]
    public void GetRoleName_WithSelfRole_ReturnsPresentedRoleNameAndSelfRoleName()
    {
        // Arrange
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

    [Test]
    public async Task GetRoleName_WithPersistedRole_ReturnsRoleName()
    {
        // Arrange
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