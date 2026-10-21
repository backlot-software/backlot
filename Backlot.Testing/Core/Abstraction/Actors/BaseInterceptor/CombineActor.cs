using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Actors.BaseInterceptor;

public class CombineActor
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void CombineActor_CombineJsonWithDictionary_NoneExistingPropertiesFromJsonObjectAreAddedToDictionary()
    {
        // Arrange
        var newObject = new Dictionary<string, object>
        {
            { "Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb" },
            { "CustomerName", "Lamborghini" }
        };

        var savedObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari",
            CardCode = "ITALY-455-ROME",
            CustomField = "Some custom lorem ipsum"
        });
        
        // Both can be presented as
        var newRole = newObject.Presents<ICard>();
        var savedRole = savedObject.Presents<ICard>();

        // Act
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(newRole.CustomerName, Is.EqualTo("Lamborghini"));
            Assert.That(newRole.CardCode, Is.EqualTo("ITALY-455-ROME"));
            
            //check for CustomField.
            Assert.That(((newRole as IProxiedRole).Actor as IDictionary<string, object>), Does.ContainKey("CustomField"));
        });
    }
    
    [Test]
    public void CombineActor_CombineDictionaryWithJson_NoneExistingPropertiesFromDictionaryAreAddedToJsonObject()
    {
        // Arrange
        var newObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari"
        });
        
        var savedObject = new Dictionary<string, object>
        {
            { "Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb" },
            { "CustomerName", "Lamborghini" },
            { "CardCode", "ITALY-455-ROME" },
            { "CustomField", "Some custom lorem ipsum" }
        };
        
        // Both can be presented as
        var newRole = newObject.Presents<ICard>();
        var savedRole = savedObject.Presents<ICard>();
        
        // Act
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(newRole.CustomerName, Is.EqualTo("Ferrari"));
            Assert.That(newRole.CardCode, Is.EqualTo("ITALY-455-ROME"));
            
            //check for CustomField.
            Assert.That(((newRole as IProxiedRole).Actor as JContainer), Does.ContainKey("CustomField"));
        });
    }
}