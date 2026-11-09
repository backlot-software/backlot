using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Testing.Defaults;
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
        Defaults.Initialize.BuildApp(builder =>
            builder.ConfigureBacklot());
    }
    
    [Test]
    public void CombineActor_CombineJsonWithDictionary_NoneExistingPropertiesFromJsonObjectAreAddedToDictionary()
    {
        // Arrange
        var newObject = new Dictionary<string, object>
        {
            { "Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb" },
            { "Number1", "1" }
        };

        var savedObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            Number1 = "2",
            Number2 = "3",
            CustomField = "Some custom lorem ipsum"
        });
        
        // Both can be presented as
        var newRole = newObject.Presents<IFormula>();
        var savedRole = savedObject.Presents<IFormula>();

        // Act
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(newRole.Number1, Is.EqualTo(1));
            Assert.That(newRole.Number2, Is.EqualTo(3));
            
            //check for CustomField.
            Assert.That(((newRole as IProxiedRole)?.Actor as IDictionary<string, object>), Does.ContainKey("CustomField"));
        });
    }
    
    [Test]
    public void CombineActor_CombineDictionaryWithJson_NoneExistingPropertiesFromDictionaryAreAddedToJsonObject()
    {
        // Arrange
        var newObject = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            Number1 = "1"
        });
        
        var savedObject = new Dictionary<string, object>
        {
            { "Uid", "8a89ca8f03db4f80a846d2e0d4b5d3cb" },
            { "Number1", "2" },
            { "Number2", "3" },
            { "CustomField", "Some custom lorem ipsum" }
        };
        
        // Both can be presented as
        var newRole = newObject.Presents<IFormula>();
        var savedRole = savedObject.Presents<IFormula>();
        
        // Act
        (newRole as IProxiedRole)?.Interceptor.CombineActor(savedRole as IProxiedRole);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(newRole.Number1, Is.EqualTo(1));
            Assert.That(newRole.Number2, Is.EqualTo(3));
            
            //check for CustomField.
            Assert.That(((newRole as IProxiedRole)?.Actor as JContainer), Does.ContainKey("CustomField"));
        });
    }
}