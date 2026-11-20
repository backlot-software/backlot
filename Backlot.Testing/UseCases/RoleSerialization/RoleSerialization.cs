using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
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
    #region Backlot Test Environment Setup
    
    [SetUp]
    public void Setup()
    {
        // This fixture contains tests needing different backlot environment configurations.
        // These setups are configured below and called in the tests //Arrange
    }

    private static void DefaultSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot());
    }
    
    private static void AliasSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new AliasDirector(
                    new FakeStorage(), config, b)));
    }
    
    private static void AliasEncryptionSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new AliasDirector(
                    new FakeStorage(), config, b),
                configureContainer: b =>
                {
                    b.Register(_ => new EncryptionService("1234567890ABCDEF"))
                        .As<IEncryptionService>()
                        .SingleInstance();
                }));
    }

    #endregion
    
    /// <summary>
    /// Verifies an aliased typed actor property survives persistence serialization and re-presentation.
    /// </summary>
    [Test]
    public void GivenTypedActorWithAliasNamedProperty_WhenSerializedForPersistenceAndPresentedAgain_ThenAliasValueIsPreserved()
    {
        // Arrange
        AliasSetup();
        
        var origin = new FormulaAlias()
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
    
    /// <summary>
    /// Verifies an aliased typed actor property survives interaction serialization and re-presentation.
    /// </summary>
    [Test]
    public void GivenTypedActorWithAliasNamedProperty_WhenSerializedForInteractionAndPresentedAgain_ThenAliasValueIsPreserved()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = new FormulaAlias()
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
    
    /// <summary>
    /// Verifies serialization across different presented roles preserves their original and added skills.
    /// </summary>
    [Test]
    public void GivenJsonRole_WhenPresentedAsDifferentRolesAndSerialized_ThenAllOriginalSkillsAndAddedSkillsArePreserved()
    {
        // Arrange
        DefaultSetup();
        
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
    
    /// <summary>
    /// Verifies merging JSON proxies preserves an explicit null and additional properties during persistence serialization.
    /// </summary>
    [Test]
    public void GivenTwoJProxiesWhereTheLeadingJsonContainsANullValueForAProperty_WhenTheJProxiesAreMergedAndSerializedForPersistence_TheNullValueAndAdditionalPropertiesArePreserved()
    {
        // Arrange
        DefaultSetup();
        
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

    #region Test Models

    /// <summary>
    /// Representing a test role using another role as a property
    /// </summary>
    public interface IPersistedOrder : IPersist
    {
        public IMoney Total { get; set; }
    }

    #endregion
}