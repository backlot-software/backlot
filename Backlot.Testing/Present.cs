using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Testing.Core;
using Backlot.Testing.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing;

/// <summary>
/// Tests on presenting a role from a source object or loaded from a database.
/// </summary>
public class Present
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Presents_OneActorPresentedTwiceAsDifferentRoles_ItActAsAIJProxy()
    {
        //NOTE: casting back from a none proxied role is not possible (except when it is a self)
        //NOTE: However when proxied, a role has it's actor inside which can be casted to the original, see; Intercepting_WithTypedActor_ActorAndProxiedValuesAreInSync

        #region ARRANGE

        string uid = "023d02600ded";

        var initial = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        #endregion

        #region ACT

        var formula = initial.Presents<IFormula>();
        var persisted = initial.Presents<IPersist>();

        #endregion

        // Always handled with a JsonInterceptor and therefor a IJProxy/
        Assert.That(formula is IJProxy);
        Assert.That(persisted is IJProxy);

        //when presenting the role, the role properties are accessable.
        Assert.That(formula.Number1 == 7);
        Assert.That(persisted.Uid == formula.Uid);
    }

    [Test]
    public async Task Presents_AnonymousActor_ProxiedRole()
    {
        #region ARRANGE

        string uid = "023d02600ded";

        var initial = new
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var repo = ServiceLocator.Get<IPersistedRoleRepository>();

        #endregion

        #region ACT

        var formula = initial.Presents<IFormula>();
        await Dummy.Play(formula);
        repo.TryGet<IFormula>(uid, out var persisted);

        #endregion

        //the proxies are not of the same type as the actor, they present the role;
        Assert.That(formula is IProxiedRole);

        //the underlying actor is of the original type.
        var actor = (formula as IProxiedRole)!.Actor;
        Assert.That(actor != null);

        //when presenting the role, the role properties are accessable.
        Assert.That(formula.Number1 == 7);

        Assert.That(formula is IProxiedRole);

        var pactor = (persisted as IProxiedRole)!.Actor;
        Assert.That(pactor != null);

        //anonymous objects aren't supported bij repos anymore. We persist them as JObjects.
        Assert.That(pactor is JObject);

        Assert.That(persisted.Number1 == 7);
    }
    
    [Test]
    public void Presents_TypedActorWithNoneMatchingPropertyTypes_NotMatchingTypesAreConvertedAnRespectedAsWhatTheRoleDefines()
    {
        #region ARRANGE
        var origin = new Formula()
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        };

        #endregion

        #region ACT
        
        var role = origin.Presents<IFormulaTypeChanged>();

        #endregion
        
        Assert.That(role.Number1 == "7");
        Assert.That(role.Number2 == "9");
    }

    [Test]
    public void Presents_DictionaryActorWithNoneMatchingUid_NotMatchingUidIsMathedToWhatFieldAliassingDefines()
    {
        var dic = new Dictionary<string, object>();
        dic.Add("BSN AN", "12355");
        dic.Add("Name", "Jeroen");

        var person = dic.Presents<IPerson>();

        Assert.That(person.Uid == "12355");
    }

}