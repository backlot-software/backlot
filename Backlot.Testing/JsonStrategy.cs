using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Core;
using NSubstitute;
using NUnit.Framework;

namespace Backlot.Testing;

public class JsonStrategy
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup((builder) => new AliasDirector(new LocalDiskStorage(), Substitute.For<IConfigurationManager>(), builder));
    }
    
    [Test]
    public void SerializeForPersistence_WithTypedActorHavingAnAliasNamedProperty_WhenJsonIsPresentedTheTypedActorHasTheSameValuesAgain()
    {
        var uid = "860C5B53";
        #region ARRANGE
        
        var origin = new FormulaDifferentAliasName()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        };
        
        #endregion
        
        #region ACT
        
        var role = origin.Presents<IFormula>();
        var jsn = role.ToJson(Strategy.SerializeForPersistance);
        var role2 = jsn.Presents<IFormula>();
        
        #endregion
        
        Assert.That(origin.Op == role.Operation);
        Assert.That(role2.Operation == origin.Op);
    }
    
    [Test]
    public void SerializeForInteraction_WithTypedActorHavingAnAliasNamedProperty_WhenJsonIsPresentedTheTypedActorHasTheSameValuesAgain()
    {
        var uid = "860C5B53";
        #region ARRANGE
        
        var origin = new FormulaDifferentAliasName()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        };
        
        #endregion
        
        #region ACT
        
        var role = origin.Presents<IFormula>();
        var jsn = role.ToJson(Strategy.SerializeForInteraction);
        var role2 = jsn.Presents<IFormula>();
        
        #endregion

        Assert.That(origin.Op == role.Operation);
        Assert.That(role2.Operation == origin.Op);
    }
    
    [Test]
    public void SerializeForPersistance_AddingSkillsWhenUsingJsonAsOriginAndPresentWithDifferentRoletypes_AllSkillPropertiesArePartOfFinalRoleDeserilization()
    {
        #region ARRANGE
        
        var origin = "{\"Name\":\"Test Order\", \"Number1\":1,\"Number2\":2,\"Operation\": \"sum\"}";
        
        #endregion
        
        #region ACT
        
        var role = origin.Presents<IFormula>();
        var jsn = role.ToJson (Strategy.SerializeForPersistance);
        
        var role2 = jsn.Presents<IPersistedOrder>();
        role2.Total = Acting.New<IMoney>();
        role2.Total.Value = (decimal)100.10;
        
        var jsn2 = role2.ToJson(Strategy.SerializeForPersistance);
        
        #endregion
        
        // this does check if all skills are part of the serialization. It does not matter in which order,
        // however this test can fail when the order is different, is allowed to optimize that.
        Assert.That(jsn.Contains("\"__Skills\":[\"NumberBase\",\"Role\",\"Persist\",\"Permission\",\"Uid\",\"Formula\"]"));
        Assert.That(jsn2.Contains("\"__Skills\":[\"Persist\",\"Permission\",\"Role\",\"Uid\",\"PersistedOrder\",\"NumberBase\",\"Formula\"]"));
    }
}