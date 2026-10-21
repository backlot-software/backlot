using System;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.Testing.Shared.Scenarios;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global : For unit testing we do not need to check this
#pragma warning disable CS8629 number should have a value for the test, therefor a safety check is not needed during testing.

namespace Backlot.Testing.UseCases.RolePersistence;

/// <summary>
/// As a Backlot developer,
/// I want to execute scenarios multiple times with roles sharing the same UID,
/// so that compatible updates are persisted correctly
/// and incompatible actor representations are rejected.
/// </summary>
public class SameUidRoleUpdates
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public async Task  GivenDifferentProxiedAndSelfRolesActorsReferringToSameUid_WhenPlayedTwice_ThenThrowsArgumentExceptionDuringMerge()
    {
        // Arrange
        const string uid = "023d02600ded";
        
        var external = new Formula() //none role actor, this actor is not implementing an IRole itself
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var self = new FormulaSelf()
        {
            Uid = uid, // refers to the same object as previous one.
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };

        var proxied = external.Presents<IFormula>();
        var presented = self.Presents<IFormula>();
        
        // Act & Assert
        await Dummy.Play(proxied);
        var exception = Assert.ThrowsAsync<ArgumentException>(() => Dummy.Play(presented));
        
        Assert.That(exception?.Message, Does.Contain("You are using a mix of origin types."));
    }
    
    [Test]
    public async Task  GivenDifferentJsonActorsReferringToSameUid_WhenPlayedTwice_ThenObjectPropertiesAreMerged()
    {
        // Arrange
        const string uid = "023d02600ded";

        var initial = JObject.FromObject(new 
        {
            Uid	= uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        
        var updated = JObject.FromObject(new  
        {
            Uid	= uid, //has the same id as initial
            Number1 = 10
        });

        var proxied = initial.Presents<IFormula>();
        var proxiedHalf = updated.Presents<IFormula>();
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        // Act
        await Dummy.Play(proxied);
        await Dummy.Play(proxiedHalf);
        repo.TryGet<IFormula>(uid, out var persisted);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(persisted.Number2.Value, Is.EqualTo(9));
            Assert.That(persisted.Number1.Value, Is.EqualTo(10));
            Assert.That(persisted.Operation, Is.EqualTo("sum"));
        });
    }
    
    [Test]
    public async Task GivenProxiedRolesWithDifferentActorTypesReferringToSameUid_WhenPlayedTwice_ThenMergeProperties()
    {
        // Arrange
        const string uid = "023d02600ded";
        
        var external = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var updated = new FormulaIncomplete()
        {
            Uid = uid, // refers to the same object as previous one.
            Number1 = 12
        };

        var proxied = external.Presents<IFormula>();
        var presented = updated.Presents<IFormula>();
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        // Act
        await Dummy.Play(proxied);
        await Dummy.Play(presented);
        repo.TryGet<IFormula>(uid, out var persisted);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(persisted.Number1, Is.EqualTo(12));
            Assert.That(persisted.Number2, Is.EqualTo(9));
            Assert.That(persisted.Operation, Is.EqualTo("sum"));
        });
    }
    
    [Test]
    public async Task GivenTypedActorsReferringToSameUid_WhenPlayedTwice_ObjectsAreMergedAndUpdatedValuesPersisted()
    {
        // Arrange
        const string uid = "023d02600ded";
        
        var initial = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var updated = new Formula()
        {
            Uid = uid, // refers to the same object as previous one.
            Number1 = 10,
            Number2 = 9,
            Operation = "sum"
        };

        var proxiedInitial = initial.Presents<IFormula>();
        var updatedInitial = updated.Presents<IFormula>();
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();

        // Act
        await Dummy.Play(proxiedInitial);
        await Dummy.Play(updatedInitial);
        repo.TryGet<IFormula>(uid, out var persisted);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(persisted.Number1, Is.EqualTo(10));
            Assert.That(persisted.Number2, Is.EqualTo(9));
            Assert.That(persisted.Operation, Is.EqualTo("sum"));
        });
    }
    
    [Test]
    public async Task GivenTypedSelfActorsReferringToSameUid_WhenPlayedTwice_ObjectsAreUpdatedAndValuesPersisted()
    {
        // Arrange
        const string uid = "023d02600ded";
        
        var self = new FormulaSelf()
        {
            Uid	= uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };
        
        var self2 = new FormulaSelf()
        {
            Uid	= uid,
            Number1 = 7,
            Number2 = 10,
            Operation = "sum"
        };
        
        var proxied = self.Presents<IFormula>();
        var proxiedHalf = self2.Presents<IFormula>();
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        // Act
        await Dummy.Play(proxied);
        await Dummy.Play(proxiedHalf);
        repo.TryGet<FormulaSelf>(uid, out var persisted);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(persisted.Number2.Value, Is.EqualTo(10));
            Assert.That(persisted.Number1.Value, Is.EqualTo(7));
            Assert.That(persisted.Operation, Is.EqualTo("sum"));
        });
    }
}