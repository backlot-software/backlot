using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing;

/// <summary>
/// Test to check if intercepting using the backlot interceptors works correctly.
/// </summary>
public class Json
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void Serialize_WithJSONActorAndSerializedForPersistance_CalculatedFieldNotSerialized()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        role.Number3 = 4;
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForPersistance);
        
        #endregion

        Assert.That(!jsn.Contains("Number3"));

    }
    
    [Test]
    public void Serialize_WithJSONActorAndSerializedForInteraction_CalculatedFieldSerialized()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        });
        var role = origin.Presents<IFormula>();
        role.Number3 = 4;

        #endregion

        #region ACT

        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(jsn.Contains("Number3"));
    }
    
    [Test]
    public void Serialize_WithTypedActorAndSerializeForInteraction_CalculatedFieldSerializedWhenCalculatedAndNotWhenOriginalyGiven()
    {
        #region ARRANGE
        var origin = new FormulaCalculatedField()
        {

            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        };

        #endregion

        #region ACT
        
        var role = origin.Presents<IFormula>();
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        role.Number3 = 7;
        var jsn2 = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        #endregion
        
        Assert.That(!jsn.Contains("Number3"));
        Assert.That(jsn2.Contains("Number3"));

    }

    [Test]
    public void Serialize_WithJSONActorWithANoneFilledChildRoleWhichIsAddedDuringExecution_ChildRoleIsCreatedUnderTheHoodAndSerializedWithTheChangedValues()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Name = "unittest",
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IOrder>();
        // Create the "child" role
        role.Total = Acting.New<IMoney>();
        // Change properties of the "Child" role
        role.Total.Value = (decimal)100.10;
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        #endregion

        // Check if both, original actor and added child role properties are serialized.
        Assert.That(role.Name == "unittest");
        Assert.That(jsn.Contains("100.1"));
    }
    
    [Test]
    public void Serialize_WithJSONActorHavingNullValueWhichIsSetByCode_ActorUpdatedWithNewValue()
    {
        #region ARRANGE
        
        // var for this json {"Number1":1,"Number2":2,"Operation":"sum"}
        var origin = "{\"Number1\":1,\"Number2\":2,\"Operation\":null}";
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        role.Operation = "sum";

        var json = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        #endregion

        Assert.That(json.Contains("\"Operation\":\"sum\""));
    }
    
    [Test]
    public void SerializeForPersistence_TwoJProxiesMergedWhereTheLeadingJsonContainsANullValueForAProperty_TheNullValueIsRespectedAfterMergeAndSerialization()
    {
        #region ARRANGE
        
        // var for this json {"Number1":1,"Number2":2,"Operation":"sum"}
        var webJson = "{\"Number1\":1,\"Number2\":2,\"Operation\":null}";
        var dbJson = "{\"Number1\":1,\"Number2\":2,\"Operation\":\"sum\",\"extra\":\"extras\"}";
        
        #endregion

        #region ACT

        var web = webJson.Presents<IFormula>();
        var db = dbJson.Presents<IFormula>();

        ((IProxiedRole)web).Interceptor.CombineActor(db as IProxiedRole);
        
        var json = web.ToJson(Strategy.SerializeForPersistance);
        #endregion

        var jobj = JObject.Parse(json);
        Assert.That(jobj["extra"]?.Value<string>() == "extras");
        Assert.That(jobj.ToString().Contains("Operation") && jobj["Operation"]?.Value<string>() == null);
    }
    
    [Test]
    public void SerializeForPersistence_WithJSONActorWhereAPropertyValueIsChangedToNull_TheNullValueIsRespectedAfterSerialization()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Number1 = 1,
            Number2 = 2,
            Operation = "sum",
            extra = "extras"
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        role.Operation = null;

        var json = role.ToJson(Strategy.SerializeForPersistance);
        #endregion
        
        var jobj = JObject.Parse(json);
        Assert.That(jobj["extra"]?.Value<string>() == "extras");
        Assert.That(jobj.ToString().Contains("Operation") && jobj["Operation"]?.Value<string>() == null);
        
    }
}