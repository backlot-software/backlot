using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing;

/// <summary>
/// Test to check if intercepting using the backlot interceptors works correctly.
/// </summary>
public class Intercepting
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Intercepting_WithJSONACTOR_ActorAndProxiedValuesAreInSync()
    {
        #region ARRANGE

        string uid = "023d02600ded";
        
        var origin = JObject.FromObject(new
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        
        var role = origin.Presents<IFormula>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion

        Assert.That(jProxy.JActor["Number1"]?.Value<int?>() == 7);
        Assert.That(role.Number1 == 7);

        role.Number1 = 8;
        
        // because json origins do work as a cloned json container the original object is not updated, however the "cloned" Origin is.
        
        // with json objects the the role and the "origin"/Actor are updated, the original object is not updated, because it is deepcloned
        // json objects do work "by 'value'".
        
        Assert.That(role.Number1 == 8);
        Assert.That(jProxy.JActor["Number1"]?.Value<int?>() == 8); 
        Assert.That(origin["Number1"]?.Value<int?>() == 8, Is.False,"JsonInterception does work with DeepCloned objects");
    }
    
    
    [Test]
    public void Intercepting_WithJSONNestedRoleACTOR_ActorAndProxiedValuesAreInSync()
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
                    Operation = "sum"
                },
                new {
                    Number1 = 17,
                    Number2 = 19,
                    Operation = "sum"
                }
            }
        });
        
        
        var role = origin.Presents<IFormulaGroup>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion

        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 7);
        Assert.That(role.Formulas.First().Number1 == 7);

        role.Formulas.First().Number1 = 8;
        
        Assert.That(origin["Formulas"][0]["Number1"]?.Value<int?>() == 8, Is.False);
        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 8);

    }
    
    [Test]
    public void Intercepting_WithJSONNestedTypedACTOR_ActorAndProxiedValuesAreInSync()
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
                    Operation = "sum"
                },
                new {
                    Number1 = 17,
                    Number2 = 19,
                    Operation = "sum"
                }
            }
        });
        
        
        var role = origin.Presents<IFormulaGroupTyped>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion

        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 7);
        var f = role.Formulas.First();
        Assert.That(f.Number1 == 7);

        role.Formulas.First().Number1 = 8;

        Assert.That(origin["Formulas"][0]["Number1"]?.Value<int?>() == 8, Is.False);
        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 8);
    }
    
    [Test]
    public void Intercepting_WithJSONNestedSelfACTOR_ActorAndProxiedValuesAreInSync()
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
                    Operation = "sum"
                },
                new {
                    Number1 = 17,
                    Number2 = 19,
                    Operation = "sum"
                }
            }
        });
        
        
        var role = origin.Presents<IFormulaGroupSelf>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion
        
        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 7);
        Assert.That(role.Formulas.First().Number1 == 7);

        role.Formulas.First().Number1 = 8;

        Assert.That(origin["Formulas"][0]["Number1"]?.Value<int?>() == 8, Is.False);
        Assert.That(jProxy.JActor["Formulas"][0]["Number1"]?.Value<int?>() == 8);
    }
    
    [Test]
    public void Intercepting_WithJSONObjectsGettingUidFromDifferentSources_WhenCardCodeIsFilledUidIsCardCode()
    {
        // virtual case is described in the comments below.
        
        #region ARRANGE

        // When created from the UI an item is created a custom generated Uid is used (no connection with the remote system yet).
        var newItem = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari"
        });
        
        // When returned from the remote api the unique identifier of the external system is returned (CardCode in this case).
        var existingItem = JObject.FromObject(new
        {
            CardCode = "C001",
            CustomerName = "Ferrari"
        });

        #endregion
        
        #region ACT
        
        // Both can be presented as

        var p1 = newItem.Presents<ICard>();
        var p2 = existingItem.Presents<ICard>();

        #endregion

        // for both objects the Uid have to be filled in.
        // In case of of the newItem Uid is equal to Uid
        // In case of existingItem Uid = CardCode
        
        Assert.That(p1.Uid == "8a89ca8f03db4f80a846d2e0d4b5d3cb");
        Assert.That(p1.CardCode == null);
        Assert.That(p2.Uid == "C001");
        Assert.That(p2.CardCode == p2.Uid);
    }
    
    [Test]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public void CombineActor_CombineJsonWithDictionary_NoneExistingPropertiesFromJsonObjectAreAddedToDictionary()
    {
        #region ARRANGE
        
        var newObject = new Dictionary<string, object>();
        newObject.Add("Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb");  
        newObject.Add("CustomerName", "Lamborghini");                 
        
        var savedObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari",
            CardCode = "ITALY-455-ROME",
            CustomField = "Some custom lorem ipsum"
        });

        #endregion
        
        #region ACT
        
        // Both can be presented as

        var newRole = newObject.Presents<ICard>();
        var savedRole = savedObject.Presents<ICard>();
        
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        #endregion
        
        Assert.That(newRole.CustomerName == "Lamborghini");
        Assert.That(newRole.CardCode == "ITALY-455-ROME");
        Assert.That(((newRole as IProxiedRole).Actor as IDictionary<string, object>).Keys.Contains("CustomField"), Is.True);
        //check for customfield.
    }
    
    [Test]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public void CombineActor_CombineDictionaryWithJson_NoneExistingPropertiesFromDictionaryAreAddedJsonObject()
    {
        #region ARRANGE
        
        var newObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari"
        });
        
        var savedObject = new Dictionary<string, object>();
        savedObject.Add("Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb");  
        savedObject.Add("CustomerName", "Lamborghini");
        savedObject.Add("CardCode", "ITALY-455-ROME");
        savedObject.Add("CustomField", "Some custom lorem ipsum");

        #endregion
        
        #region ACT
        
        // Both can be presented as

        var newRole = newObject.Presents<ICard>();
        var savedRole = savedObject.Presents<ICard>();
        
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        #endregion
        
        Assert.That(newRole.CustomerName == "Ferrari");
        Assert.That(newRole.CardCode == "ITALY-455-ROME");
        //Assert.That(((newRole as IProxiedRole).Actor as JContainer).Con.Keys.Contains("CustomField"), Is.True);
        //check for customfield.
    }
}