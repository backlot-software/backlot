using System;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Exceptions;
using Backlot.Core.Security;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing;

/*
 * DEFINITIONS;
 * Namingconvention; MethodNameToTest_StateUnderTest_ExpectedBehavior
 * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
 * Dummy - objects are passed around but never actually used. Usually they are just used to fill parameter lists.
 * Fake - objects actually have working implementations, but usually take some shortcut which makes them not suitable for production (an in memory database is a good example).
 * Stubs - provide canned answers to calls made during the test, usually not responding at all to anything outside what's programmed in for the test. Stubs may also record information about calls, such as an email gateway stub that remembers the messages it 'sent', or maybe only how many messages it 'sent'.
 * Mocks - are what we are talking about here: objects pre-programmed with expectations which form a specification of the calls they are expected to receive
 */

public class Persist
{

    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Persist_NoPermissionSet_PermissionException()
    {
        #region ARRANGE

        var jObject = JObject.FromObject(new
        {
            Uid = "UnitTestObject-" + Uid.New(),
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromJObject = jObject.Presents<IFormula>();
        fromJObject.ManagePermission(p => p.SetMask(PermissionLevel.None));

        var repo = new PersistedRoleRepositoryStub();

        #endregion

        #region ACT & ASSERT

        

        Assert.Throws<PermissionControlException>(() =>
        {
            try
            {
                repo.Persist(fromJObject).Wait();
            }
            catch (Exception e)
            {
#pragma warning disable CS8597 // Thrown value may be null.
                throw e.InnerException;
#pragma warning restore CS8597 // Thrown value may be null.
            }
        });
        Assert.That(0, Is.EqualTo(repo.StoreCallCount));

        #endregion
    }
    
    [Test]
    public void Persist_MaskLevelTo7ButNoUserOrGroupWithThatPermission_PermissionException()
    {
        #region ARRANGE

        var jObject = JObject.FromObject(new
        {
            Uid = "UnitTestObject-" + Uid.New(),
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        var fromJObject = jObject.Presents<IFormula>();
        fromJObject.ManagePermission(p => 
            p.SetMask(PermissionLevel.ReadWriteExecute)
                .SetGroup("Admin", PermissionLevel.None));

        var repo = new PersistedRoleRepositoryStub();

        #endregion

        #region ACT & ASSERT

        Assert.Throws<PermissionControlException>(() =>
        {
            try
            {
                repo.Persist(fromJObject).Wait();
            }
            catch (Exception e)
            {
#pragma warning disable CS8597 // Thrown value may be null.
                throw e.InnerException;
#pragma warning restore CS8597 // Thrown value may be null.
            }
        });
        
        // no exception is thrown.
        Assert.That(0, Is.EqualTo(repo.StoreCallCount));

        #endregion
    }
    
    [Test]
    public async Task Persist_MaskLevel7AndAtLeastOneUserSetWithALevelHigherThan0_NoPermissionExceptionThrown()
    {
        #region ARRANGE

        var jObject = JObject.FromObject(new
        {
            Uid = "UnitTestObject-" + Uid.New(),
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
        await repo.Persist(fromJObject);
        
        #endregion

        #region ACT & ASSERT

        // no exception is thrown.
        Assert.That(1, Is.EqualTo(repo.StoreCallCount));

        #endregion
    }

    [Test]
    public async Task Persist_UpdateWritableObject_UpdatedIFormula()
    {
        #region ARRANGE

        var uid = "UnitTestObject-" + Uid.New();

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

        #endregion

        #region ACT

        var result = await repo.Persist(fromUpdate);

        #endregion

        #region ASSERT

        //check succesfull merge on the changed properties
        Assert.That(fromUpdate.Number1, Is.EqualTo(result.Number1));
        Assert.That(fromUpdate.Operation, Is.EqualTo(result.Operation));

        //new permission should be leading
        Assert.That(fromUpdate.__Permission, Is.EqualTo(result.__Permission));

        //check succesfull merge on unchanged properties
        Assert.That(fromStored.Number2, Is.EqualTo(result.Number2));
        Assert.That(fromStored.Uid, Is.EqualTo(result.Uid));

        //store should be called 1 time
        Assert.That(1, Is.EqualTo(repo.StoreCallCount));

        #endregion
    }

    [Test]
    public void Persist_UpdateReadOnlyObject_PermissionException()
    {
        #region ARRANGE

        var uid = "UnitTestObject-" + Uid.New();

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

        #endregion

        #region ACT & ASSERT

        Assert.Throws<PermissionControlException>(() =>
        {
            try
            {
                repo.Persist(fromUpdate).Wait();
            }
            catch (Exception e)
            {
#pragma warning disable CS8597 // Thrown value may be null.
                throw e.InnerException;
#pragma warning restore CS8597 // Thrown value may be null.
            }
        });
        
        Assert.That(0, Is.EqualTo(repo.StoreCallCount));

        #endregion
    }

    [Test]
    public async Task Persist_StoreNewWriteableObject_IFormula()
    {
        #region ARRANGE

        var newObject = JObject.FromObject(new
        {
            Uid = "UnitTestObject-" + Uid.New(),
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

        #endregion

        #region ACT

        var result = await repo.Persist(fromNewObject);

        #endregion

        #region ASSERT

        Assert.That(fromNewObject, Is.EqualTo(result));

        #endregion
    }

    [Test]
    public async Task Persist_PersistedObject_IFormula()
    {
        #region ARRANGE

        var jObject = JObject.FromObject(new
        {
            Uid = "UnitTestObject-" + Uid.New(),
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

        #endregion

        #region ACT

        var result = await repo.Persist(fromJObject);

        #endregion

        #region ASSERT

        Assert.That(fromJObject, Is.EqualTo(result));

        #endregion
    }
}
