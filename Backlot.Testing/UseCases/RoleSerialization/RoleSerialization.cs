using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.UseCases.RoleSerialization;

/// <summary>
/// As a Backlot developer,
/// I want to serialize and re-present roles using different serialization strategies,
/// so that aliased properties, role skills and explicit null values are preserved.
/// </summary>
public class RoleSerialization
{
    // TODO: Move alias-specific test initialization to a shared helper
    // in the planned Backlot.Testing.Defaults NuGet package.
    // Also review how to avoid duplicate Initialize.Setup() calls.
    [SetUp]
    public void Setup()
    {
        Initialize.Setup((builder) => new AliasDirector(new LocalDiskStorage(), Substitute.For<IConfigurationManager>(), builder));
    }
    
    [Test]
    public void GivenTypedActorWithAliasNamedProperty_WhenSerializedForPersistenceAndPresentedAgain_ThenAliasValueIsPreserved()
    {
        // Arrange
        var origin = new FormulaDifferentAliasName()
        {
            Uid = "860C5B53",
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        };
        
        // Act
        var role = origin.Presents<IFormula>();
        var jsn = role.ToJson(Strategy.SerializeForPersistance);
        var role2 = jsn.Presents<IFormula>();
        
        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(origin.Op, Is.EqualTo(role.Operation));
            Assert.That(role2.Operation, Is.EqualTo(origin.Op)); 
        });
    }
    
    [Test]
    public void GivenTypedActorWithAliasNamedProperty_WhenSerializedForInteractionAndPresentedAgain_ThenAliasValueIsPreserved()
    {
        // Arrange
        var origin = new FormulaDifferentAliasName()
        {
            Uid = "860C5B53",
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        };
        
        // Act
        var role = origin.Presents<IFormula>();
        var jsn = role.ToJson(Strategy.SerializeForInteraction);
        var role2 = jsn.Presents<IFormula>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(origin.Op, Is.EqualTo(role.Operation));
            Assert.That(role2.Operation, Is.EqualTo(origin.Op));
        });
    }
    
    [Test]
    public void GivenJsonRole_WhenPresentedAsDifferentRolesAndSerialized_ThenAllOriginalSkillsAndAddedSkillsArePreserved()
    {
        // Arrange
        var origin = "{\"Name\":\"Test Order\", \"Number1\":1,\"Number2\":2,\"Operation\": \"sum\"}";
        
        // Act
        var formula = origin.Presents<IFormula>();
        var formulaJsn = formula.ToJson (Strategy.SerializeForPersistance);
        
        var order = formulaJsn.Presents<IPersistedOrder>();
        order.Total = Acting.New<IMoney>();
        order.Total.Value = (decimal)100.10;
        var orderJson = order.ToJson(Strategy.SerializeForPersistance);
        
        //Assert
        Assert.Multiple(() =>
        {
            // this does check if all skills are part of the serialization. It does not matter in which order,
            // however this test can fail when the order is different, is allowed to optimize that.
            Assert.That(formulaJsn, Does.Contain("\"__Skills\":[\"NumberBase\",\"Role\",\"Persist\",\"Permission\",\"Uid\",\"Formula\"]"));
            Assert.That(orderJson, Does.Contain("\"__Skills\":[\"Persist\",\"Permission\",\"Role\",\"Uid\",\"PersistedOrder\",\"NumberBase\",\"Formula\"]"));
        });
    }
    
    [Test]
    public void GivenTwoJProxiesWhereTheLeadingJsonContainsANullValueForAProperty_WhenTheJProxiesAreMergedAndSerializedForPersistence_TheNullValueAndAdditionalPropertiesArePreserved()
    {
        // Arrange
        
        // var for this json {"Number1":1,"Number2":2,"Operation":"sum"}
        var webJson = "{\"Number1\":1,\"Number2\":2,\"Operation\":null}";
        var dbJson = "{\"Number1\":1,\"Number2\":2,\"Operation\":\"sum\",\"extra\":\"extras\"}";
        var web = webJson.Presents<IFormula>();
        var db = dbJson.Presents<IFormula>();
        
        // Act
        ((IProxiedRole)web).Interceptor.CombineActor(db as IProxiedRole);
        
        var json = web.ToJson(Strategy.SerializeForPersistance);
        var result = JObject.Parse(json);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["extra"]?.Value<string>(), Is.EqualTo("extras"));
            Assert.That(result.ToString(), Does.Contain("Operation"));
            Assert.That(result["Operation"]?.Value<string>() , Is.Null);
        });
    }
}