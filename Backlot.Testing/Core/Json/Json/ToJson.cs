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

namespace Backlot.Testing.Core.Json.Json;

/// <summary>
/// Tests JSON serialization of roles using different
/// serialization strategies and property values.
/// </summary>
public class ToJson
{
    #region Backlot Test Environment Setup

    private static void DefaultSetup()
    {
        Initialize.BuildApp(builder => 
            builder.ConfigureBacklot());
    }

    private static void PermissionEncryptionSetup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b),
                configureContainer: b =>
                {
                    b.Register(_ => new EncryptionService("1234567890ABCDEF"))
                        .As<IEncryptionService>()
                        .SingleInstance();
                }));
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
    
    [Test]
    public void ToJson_WithJSONActorAndSerializedForPersistence_CalculatedFieldNotSerialized()
    {
        // Arrange
        DefaultSetup();
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        var role = origin.Presents<IFormula>();
        role.Number3 = 4;

        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForPersistance);
        
        // Assert
        Assert.That(jsn, Does.Not.Contain("Number3"));
    }
    
    [Test]
    public void ToJson_WithJSONActorAndSerializedForInteraction_CalculatedFieldSerialized()
    {
        // Arrange
        PermissionEncryptionSetup();
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        });
        var role = origin.Presents<IFormula>();
        role.Number3 = 4;

        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.That(jsn,  Does.Contain("Number3"));
    }
    
    [Test]
    public void ToJson_WithTypedActorWithUpdatedCalculatedFieldAndSerializeForInteraction_CalculatedFieldSerialized()
    {
        // Arrange
        PermissionEncryptionSetup();
        
        var origin = new FormulaCalculatedField()
        {

            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        };
        
        var role = origin.Presents<IFormula>();
        // update calculated field
        role.Number3 = 7;

        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.That(jsn, Does.Contain("Number3"));
    }
    
    [Test]
    public void ToJson_WithTypedActorWithCalculatedFieldOriginallyGivenAndSerializeForInteraction_CalculatedFieldIsNotSerialized()
    {
        // Arrange
        PermissionEncryptionSetup();
        
        var origin = new FormulaCalculatedField()
        {

            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        };
        
        var role = origin.Presents<IFormula>();

        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.That(jsn, Does.Not.Contain("Number3"));
    }
    
    [Test]
    public void ToJson_WithJSONActorWithANoneFilledChildRoleWhichIsAddedDuringExecution_ChildRoleIsCreatedUnderTheHoodAndSerializedWithTheChangedValues()
    {
        // Arrange
        DefaultSetup();
        
        var origin = JObject.FromObject(new
        {
            Name = "unittest",
        });
        var role = origin.Presents<IOrder>();
        
        // Create the "child" role
        role.Total = Acting.New<IMoney>();
        // Change properties of the "Child" role
        role.Total.Value = (decimal)100.10;
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.Multiple(() =>
        {
            // Check if both, original actor and added child role properties are serialized.
            Assert.That(role.Name, Is.EqualTo("unittest"));
            Assert.That(jsn, Does.Contain("100.1"));
        });
    }
    
    [Test]
    public void ToJson_WithJSONActorHavingNullValueWhichIsSetByCodeAndUpdatedWithNewValue_NewValueSerialized()
    {
        // Arrange
        PermissionEncryptionSetup();
        
        // var for this json {"Number1":1,"Number2":2,"Operation":"sum"}
        var origin = "{\"Number1\":1,\"Number2\":2,\"Operation\":null}";
        var role = origin.Presents<IFormula>();
        
        // update Null value
        role.Operation = "sum";
        
        // Act
        var json = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.That(json, Does.Contain("\"Operation\":\"sum\""));
    }
    
    [Test]
    public void ToJson_WithJSONActorWhereAPropertyValueIsChangedToNullAndSerializedForPersistence_TheNullValueIsRespectedAfterSerialization()
    {
        // Arrange
        DefaultSetup();
        
        var origin = JObject.FromObject(new
        {
            Number1 = 1,
            Number2 = 2,
            Operation = "sum",
            extra = "extras"
        });
        var role = origin.Presents<IFormula>();
        
        // update value to null
        role.Operation = null;
        
        // Act
        var json = role.ToJson(Strategy.SerializeForPersistance);
        var jsnParsed = JObject.Parse(json);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(jsnParsed["extra"]?.Value<string>(), Is.EqualTo("extras"));
            Assert.That(jsnParsed.ToString(), Does.Contain("Operation"));
            Assert.That(jsnParsed["Operation"]?.Value<string>(), Is.Null);
        });
    }
    
    [Test]
    public void ToJson_WithJSONActorUsingAnAliasNamedProperty_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Op = "sum",
        });
        
        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(jsn, Does.Not.Contain("\"Op\""));
            Assert.That(jsn, Does.Contain("\"Operation\""));
        });
    }
    
    [Test]
    public void ToJson_WithJSONActorUsingAnAliasMarkedPropertyWithoutAnAliasName_NormalFieldNameUsedInSerialization()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(jsn, Does.Not.Contain("\"Op\""));
            Assert.That(jsn, Does.Contain("\"Operation\""));
        });
    }
    
    [Test]
    public void ToJson_WithTypedActorUsingAnAliasNamedProperty_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = new FormulaAlias
        {
            Number1 = 7,
            Number2 = 9,
            Op = "sum", // the actor name
        };
        
        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        var result = JObject.Parse(jsn);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["Op"], Is.Null); // actor name removed from root
            Assert.That(result["Operation"], Is.Not.Null);
        });
    }
    
    [Test]
    public void ToJson_WithTypedActorUsingAnAliasMarkedPropertyWithoutAnAliasName_ActorNameUsedRoleDefinedNameIgnoredInSerialization()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = new Formula()
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        };
        
        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(jsn, Does.Not.Contain("\"Op\""));
            Assert.That(jsn, Does.Contain("\"Operation\""));
        });
    }
    
    [Test]
    public void ToJson_WithJSONActorUsingAnAliasDefinedPropertyUsingFieldInfoAlias_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        // Arrange
        AliasEncryptionSetup();
        
        var origin = JObject.FromObject(new
        {
            Id = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        var role = origin.Presents<IFormula>();
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Uid, Is.EqualTo("023d02600ded"));
            Assert.That(jsn, Does.Not.Contain("\"Id\"")); // is NOT serialized because its used by Uid
            Assert.That(jsn, Does.Contain("\"Uid\""));
        });
    }
    
    [Test]
    public void ToJson_WithJSONActorHavingTheOriginalPropertyAndAnAliasPropertyAndSerializeForPersistenceStrategy_RoleIsUsingTheOriginalAsDefault()
    {
        // Arrange
        AliasSetup();
        
        var origin = JObject.FromObject(new
        {
            Uid = "123", // default
            Id = "023d02600ded", // skipped but serialized
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        var role = origin.Presents<IFormula>();
        
        // Act
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForPersistance);
        var jsnParsed = JObject.Parse(jsn);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Uid, Is.EqualTo("123"));
            Assert.That(jsnParsed["Id"], Is.Not.Null); // is also serialized, because it's skipped during aliassing and therefor NOT used by Uid.
            Assert.That(jsnParsed["Uid"], Is.Not.Null);
        });
    }

    #region Test Models

    public class FormulaCalculatedField
    {
        public string HelloWorld => "HelloWorld";
        public string Operation { get; set; } = null!;
        public int? Number1 { get; set; }
        public int? Number2 { get; set; }
        public int? Number3 { get; set; }

        public string Name => "Example which can presents it self as an IFormula Role";

        public string Uid { get; set; } = null!;
    }
    
    /// <summary>
    /// Representing a test role using another role as a property
    /// </summary>
    public interface IOrder : IRole
    {
        public string Name { get; set; }
        public IMoney Total { get; set; }
    }

    #endregion
}