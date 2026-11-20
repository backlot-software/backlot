using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Exceptions;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
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
    #region Backlot Test Environment Setup
    
    [SetUp]
    public void Setup()
    {
        // This fixture contains tests needing different backlot environment configurations.
        // These setups are configured below and called in the tests //Arrange
    }

    private static void PermissionChecksumSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b),
                configureContainer: b =>
                {
                    b.Register(_ => new ChecksumBuilder(MD5.Create().ComputeHash))
                        .As<IChecksumBuilder>();
                }));
    }
    
    private static void PermissionSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b)));
    }

    #endregion

    /// <summary>
    /// Verifies a role with no permissions cannot be persisted and is not stored.
    /// </summary>
    [Test]
    public void Persist_RoleWithNonePermissionSet_ThrowsPermissionControlException()
    {
        // Arrange
        PermissionSetup();
        
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
    
    /// <summary>
    /// Verifies a nonzero permission mask without a matching user or group permission prevents persistence.
    /// </summary>
    [Test]
    public void Persist_MaskPermissionLevelTo7ButNoUserOrGroupWithThatPermission_ThrowsPermissionControlException()
    {
        // Arrange
        PermissionSetup();
        
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
    
    /// <summary>
    /// Verifies a role with a user granted permission can be persisted.
    /// </summary>
    [Test]
    public async Task Persist_MaskPermissionLevel7AndAtLeastOneUserSetWithALevelHigherThan0_RoleIsStored()
    {
        // Arrange
        PermissionSetup();
        
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

    /// <summary>
    /// Verifies updating a writable role merges changed and unchanged properties and stores the result.
    /// </summary>
    [Test]
    public async Task Persist_UpdateWritableObject_MergesUpdatedAndExistingProperties()
    {
        // Arrange
        PermissionChecksumSetup();
        
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

    /// <summary>
    /// Verifies updating a read-only persisted role throws a permission exception without storing it.
    /// </summary>
    [Test]
    public void Persist_UpdateReadOnlyObject_ThrowsPermissionControlException()
    {
        // Arrange
        PermissionChecksumSetup();
        
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

    /// <summary>
    /// Verifies a new writable role is stored and returned by persistence.
    /// </summary>
    [Test]
    public async Task Persist_StoreNewWriteableObject_StoresAndReturnsObject()
    {
        // Arrange
        PermissionSetup();
        
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

    /// <summary>
    /// Verifies persisting an unchanged stored role returns it without storing it again.
    /// </summary>
    [Test]
    public async Task Persist_UnchangedPersistedObject_ReturnsWithoutStoringAgain()
    {
        // Arrange
        PermissionChecksumSetup(); //todo configure proper setup
        
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
