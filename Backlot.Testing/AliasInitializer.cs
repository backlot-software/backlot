using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Dynamic;
using System.Linq;
using Backlot.Core.Json.Serialization.Newtonsoft;
using NSubstitute;

namespace Backlot.Testing;

public class AliasInitializer
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup((builder) => new AliasDirector(new LocalDiskStorage(), Substitute.For<IConfigurationManager>(), builder));
    }
    
    // ================================================== INTERCEPTING =========================================================================
    
    [Test]
    public void Intercepting_WithJSONActorUsingAliasses_AliasIsUpdatedInRoleAndDeepClonedOrigin()
    {
        #region ARRANGE

        string uid = "023d02600ded";
        
        
        
        var origin = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        });
        
        var role = origin.Presents<IFormula>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion
        
        #region ACT
        
        role.Operation = "new";
        role.Number3 = 99;
        
        #endregion
        
        // with typed objects the original object as well as the role and the "origin"/Actor are updated
        // typed objects do work "by reference"
        
        Assert.That(role.Operation == "new");

        Assert.That(jProxy.JActor["Op"]?.Value<string?>() == "new"); 
        Assert.That(origin["Op"]?.Value<string?>() == "new", Is.False, "JsonInterception does work with DeepCloned objects");
        Assert.That(role.Number3 == 99);
    }
    
    [Test]
    public void Intercepting_WithJSONNestedRoleACTORWhichNeedsAliassing_AliasValueIsUsedForRoleProperty()
    {
        #region ARRANGE

        string uid = "5DEB1636";

        var origin = JObject.FromObject(new
        {
            Uid = uid,
            Formulas = new [] { 
                new {
                    Number1 = 7,
                    Number2 = 9,
                    Op = "sum" // alias for Operation.
                },
                new {
                    Number1 = 17,
                    Number2 = 19,
                    Op = "sum"
                }
            }
        });
        
        
        var role = origin.Presents<IFormulaGroup>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion

        Assert.That(role.Formulas.First().Operation == "sum"); // "Op" alias is used for "Operation" value.
    }
    
    // =================================================== JSON =============================================================
    
    [Test]
    public void Serialize_WithJSONActorUsingAnAliasNamedProperty_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Op = "sum",
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(jsn.Contains("\"Op\""), Is.False);
        Assert.That(jsn.Contains("\"Operation\""));
    }
    
    [Test]
    public void Serialize_WithJSONActorUsingAnAliasMarkedPropertyWithoutAnAliasName_NormalFieldNameUsedInSerialization()
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
        role.Operation = "new";
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(jsn.Contains("\"Op\""), Is.False);
        Assert.That(jsn.Contains("\"Operation\""));
    }
    
    [Test]
    public void Serialize_WithTypedActorUsingAnAliasNamedProperty_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        #region ARRANGE
        
        var origin = new FormulaAlias
        {
            Number1 = 7,
            Number2 = 9,
            Op = "sum", // the actor name
        };
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(JObject.Parse(jsn)["Op"] == null); // actor name removed from root
        Assert.That(JObject.Parse(jsn)["Operation"] != null);
    }
    
    [Test]
    public void Serialize_WithTypedActorUsingAnAliasMarkedPropertyWithoutAnAliasName_ActorNameUsedRoleDefinedNameIgnoredInSerialization()
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

        var role = origin.Presents<IFormula>();
        role.Operation = "new";
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(jsn.Contains("\"Op\""), Is.False);
        Assert.That(jsn.Contains("\"Operation\""));
    }
    
    [Test]
    public void Serialize_WithJSONActorUsingAnAliasDefinedPropertyUsinFieldInfoAlias_RolePropertyNameUsedActorNameRemovedInSerialization()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Id = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForInteraction);
        
        #endregion

        Assert.That(role.Uid == "023d02600ded");
        Assert.That(jsn.Contains("\"Id\""), Is.False); // is NOT serialized because its used by Uid
        Assert.That(jsn.Contains("\"Uid\""));
        
    }
    
    [Test]
    public void Serialize_WithJSONActorHavingTheOriginalPropertyAndAnAliasPropertyWithSerializeForPersistenceStrategy_RoleIsUsingTheOriginalAsDefault()
    {
        #region ARRANGE
        
        var origin = JObject.FromObject(new
        {
            Uid = "123", // default
            Id = "023d02600ded", // skipped but serialized
            Number1 = 7,
            Number2 = 9,
            Number3 = 4,
            Operation = "sum",
        });
        
        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();
        
        var jsn = Backlot.Core.Json.Json.ToJson(role, Strategy.SerializeForPersistance);
        
        #endregion

        Assert.That(role.Uid == "123");
        Assert.That(JObject.Parse(jsn)["Id"] != null); // is also serialized, because its skipped during aliassing and there for NOT used by Uid.
        Assert.That(JObject.Parse(jsn)["Uid"] != null);
        
    }

    /// <summary>
    /// Assumption FH:
    /// Because the <see cref="IFormula.Operation"/> has an alias dictionary that contains the string "Operatie", this test should succeed.
    /// create item for in basecamp
    /// </summary>
    [Test]
    public void Presents_AnonymousActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        #region ARRANGE

        var origin = new
        {
            Operatie = "sum",
        };

        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();

        #endregion

        Assert.That(role.Operation == "sum");
    }

    [Test]
    public void
        Presents_DictionaryActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        #region ARRANGE

        var origin = new Dictionary<string, object>();
        origin["Operatie"] = "sum";

        #endregion

        #region ACT

        var role = origin.Presents<IFormula>();

        #endregion

        Assert.That(role.Operation == "sum");
    }

    [Test]
    public void Presents_ExpandoObjectActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        #region ARRANGE

        dynamic origin = new ExpandoObject();

        origin.Operatie = "sum";
        #endregion

        #region ACT

        var role = Acting.Presents<IFormula>(origin);

        #endregion

        Assert.That(role.Operation == "sum");
    }
    
    
}