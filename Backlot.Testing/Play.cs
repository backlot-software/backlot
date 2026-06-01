using System;
using System.Threading.Tasks;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Testing.Core;
using Backlot.Testing.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global : For unit testing we do not need to check this
#pragma warning disable CS8629 number should have a value for the test, therefor a safety check is not needed during testing.

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

/// <summary>
/// Tests playing scenarios and testing on real-life programming use-cases
/// </summary>
public class Play
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public async Task PlayAndGet_UsingASelfRole_RoleIsNotProxiedAndIsSameTypeAsArrangedObjectWhenLoadedFromRepo()
    {
        //A self object is an object implementing a role interface itself. 
        
        #region ARRANGE
        
        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        #endregion
        
        #region ACT
        
        var presented = self.Presents<IFormula>();
        await Dummy.Play(presented);
        repo.TryGet<IFormula>(self.Uid, out var dbentity);
        
        #endregion
        
        Assert.That(presented is not IProxiedRole, $"The self instance did presents as a proxied role, which is not allowed / needed.");
        Assert.That(presented is FormulaSelf, $"The self instance did not presents it self a type of the same instance");
        Assert.That(dbentity is FormulaSelf, $"The self instance did not presents it self a type of the same instance when loaded from the dbrepo");
    }
    
    [Test]
    public async Task  PlayTwice_DifferentTypeActorsReferringToSameUid_ArgumentExceptionDuringMerge()
    {
        #region ARRANGE

        var uid = "023d02600ded";
        
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

        #endregion

        #region ACT & ASSERT;
        
        try
        {
            await Dummy.Play(proxied);
            await Dummy.Play(presented);
        }
        catch (ArgumentException ex)
        {
            Assert.That(ex.Message.Contains($"You are using a mix of origin types."));
            Assert.Pass();
        }
        
        Assert.Fail(
            $"A persited role is presented as self '{self.GetType().FullName}', but initialy stored and represented as a {nameof(IJProxy)}. This can not be combined during {nameof(BasePersistedRoleRepository.Persist)}");
        
        #endregion
    }

    [Test]
    public async Task  PlayTwice_DifferentJsonActorsReferringToSameUid_ObjectsAreMerged()
    {
        #region ARRANGE

        var uid = "023d02600ded";

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
        
        #endregion
        
        #region ACT

        await Dummy.Play(proxied);
        await Dummy.Play(proxiedHalf);

        #endregion
        
        repo.TryGet<IFormula>(uid, out var persisted);

        Assert.That(persisted.Number2.Value == 9);
        Assert.That(persisted.Number1.Value == 10);

        Assert.That(persisted.Operation == "sum");
    }

    [Test]
    public async Task PlayTwice_DifferentTypeNoneRoleActorsReferringToSameUid_ArgumentExceptionDuringMerge()
    {
        #region ARRANGE

        var external = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var self = new FormulaIncomplete()
        {
            Uid = "023d02600ded", // refers to the same object as previous one.
            Number1 = 12
        };

        var proxied = external.Presents<IFormula>();
        var presented = self.Presents<IFormula>();

        #endregion

        #region ACT

        try
        {
            await Dummy.Play(proxied);
            await Dummy.Play(presented);
        }
        catch (Exception)
        {
            Assert.Fail("During Repository.Persist a ActorCombine has to be executed, this Combine has to succeed because sinde 2.2.0 we are supporting combining 2 different types.");
        }

        Assert.Pass();

        #endregion
    }

    [Test]
    public async Task PlayTwice_TypedActorsReferringToSameUid_ObjectsAreMerged()
    {
        #region ARRANGE

        string uid = "023d02600ded";
        
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

        #endregion

        #region ACT

        await Dummy.Play(proxiedInitial);
        await Dummy.Play(updatedInitial);
        repo.TryGet<IFormula>(uid, out var persisted);
        
        #endregion
        
        Assert.That(persisted.Number1 is 10);
        Assert.That(persisted.Number2 is 9);
        Assert.That(persisted.Operation == "sum");
    }
    
    [Test]
    public async Task PlayTwice_TypedSelfActorsReferringToSameUid_ObjectsAreUpdated()
    {
        #region ARRANGE

        string uid = "023d02600ded";
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

        var repo = ServiceLocator.Get<IPersistedRoleRepository>();
        
        var proxied = self.Presents<IFormula>();
        var proxiedHalf = self2.Presents<IFormula>();
        
        #endregion
        
        #region ACT

        await Dummy.Play(proxied);
        await Dummy.Play(proxiedHalf);
        
        #endregion

        repo.TryGet<FormulaSelf>(uid, out var persisted);

        Assert.That(persisted.Number2.Value == 10);
        Assert.That(persisted.Number1.Value == 7);

        Assert.That(persisted.Operation == "sum");
    }

    public async Task Play_Fluent()
    {
        throw new NotImplementedException("Fluent api need to be tested.");
    }
}