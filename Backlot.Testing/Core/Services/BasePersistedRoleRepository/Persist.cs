using System;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Exceptions;
using Backlot.Core.Security;
using Backlot.Testing.Shared.Stubs;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.Core.Services.BasePersistedRoleRepository;

/// <summary>
/// Tests role persistence, including permission validation,
/// updates, and storage behavior.
/// </summary>
public class Persist
{

    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Persist_RoleWithNonePermissionSet_ThrowsPermissionControlException()
    {
        // Arrange
        var jObject = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromJObject = jObject.Presents<IFormula>();
        fromJObject.ManagePermission(p => p.SetMask(PermissionLevel.None));

        var repo = new PersistedRoleRepositoryStub();

        // Act & Assert
        Assert.ThrowsAsync<PermissionControlException>(
            async () => await repo.Persist(fromJObject));
        
        Assert.That(repo.StoreCallCount, Is.EqualTo(0));
    }
    
    [Test]
    public void Persist_MaskPermissionLevelTo7ButNoUserOrGroupWithThatPermission_ThrowsPermissionControlException()
    {
        // Arrange
        var jObject = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromJObject = jObject.Presents<IFormula>();
        fromJObject.ManagePermission(p => 
            p.SetMask(PermissionLevel.ReadWriteExecute)
                .SetGroup("Admin", PermissionLevel.None));

        var repo = new PersistedRoleRepositoryStub();

        // Act & Assert
        Assert.ThrowsAsync<PermissionControlException>(
            async () => await repo.Persist(fromJObject));
        
        Assert.That(repo.StoreCallCount, Is.EqualTo(0));
    }
    
    [Test]
    public async Task Persist_MaskPermissionLevel7AndAtLeastOneUserSetWithALevelHigherThan0_RoleIsStored()
    {
        // Arrange
        var jObject = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromJObject = jObject.Presents<IFormula>();
        fromJObject.ManagePermission(p => 
            p.SetMask(PermissionLevel.ReadWriteExecute)
                .SetGroup("Admin", PermissionLevel.None)
                .SetUser("foo", PermissionLevel.ReadWriteExecute));

        var repo = new PersistedRoleRepositoryStub();
        
        // Act
        await repo.Persist(fromJObject);

        // Assert
        Assert.That(repo.StoreCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Persist_UpdateWritableObject_MergesUpdatedAndExistingProperties()
    {
        // Arrange
        const string uid = "023d02600ded";

        var stored = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromStored = stored.Presents<IFormula>();

        var update = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 80,
            Operation = "squared"
        });
        var fromUpdate = update.Presents<IFormula>();

        var repo = new PersistedRoleRepositoryStub()
        {
            SetPersisted = false,
            SetTryGet = true,
            TryGetOutRole = fromStored
        };

        // Act
        var result = await repo.Persist(fromUpdate);

        // Assert
        Assert.Multiple(() =>
        {
            //check successful merge on the changed properties
            Assert.That(fromUpdate.Number1, Is.EqualTo(result.Number1));
            Assert.That(fromUpdate.Operation, Is.EqualTo(result.Operation));

            //new permission should be leading
            Assert.That(fromUpdate.__Permission, Is.EqualTo(result.__Permission));

            //check successful merge on unchanged properties
            Assert.That(fromStored.Number2, Is.EqualTo(result.Number2));
            Assert.That(fromStored.Uid, Is.EqualTo(result.Uid));

            //store should be called 1 time
            Assert.That(repo.StoreCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Persist_UpdateReadOnlyObject_ThrowsPermissionControlException()
    {
        // Arrange
        const string uid = "023d02600ded";

        var stored = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromStored = stored.Presents<IFormula>();
        // set different mask level:
        fromStored.ManagePermission(p => p.SetMask(PermissionLevel.ReadExecute));

        var update = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 80,
            Number2 = 10,
            Operation = "squared"
        });
        var fromUpdate = update.Presents<IFormula>();

        var repo = new PersistedRoleRepositoryStub()
        {
            SetPersisted = false,
            SetTryGet = true,
            TryGetOutRole = fromStored
        };

        // Act & Assert
        Assert.ThrowsAsync<PermissionControlException>(
            async () => await repo.Persist(fromUpdate));
        
        Assert.That(repo.StoreCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Persist_StoreNewWriteableObject_StoresAndReturnsObject()
    {
        // Arrange
        var newObject = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromNewObject = newObject.Presents<IFormula>();

        var repo = new PersistedRoleRepositoryStub()
        {
            SetPersisted = false,
            SetTryGet = false
        };

        // Act
        var result = await repo.Persist(fromNewObject);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(repo.StoreCallCount,  Is.EqualTo(1));
            Assert.That(fromNewObject, Is.EqualTo(result));
        });
    }

    [Test]
    public async Task Persist_UnchangedPersistedObject_ReturnsWithoutStoringAgain()
    {
        // Arrange
        var jObject = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            __Permission = Permission.Create(PermissionLevel.ReadWriteExecute).ToString(),
        });
        var fromJObject = jObject.Presents<IFormula>();

        var repo = new PersistedRoleRepositoryStub()
        {
            SetPersisted = true,
            SetTryGet = true,
            TryGetOutRole = fromJObject
        };

        // Act
        var result = await repo.Persist(fromJObject);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(fromJObject, Is.EqualTo(result));
            Assert.That(repo.StoreCallCount,  Is.EqualTo(0));
        });
    }
}
