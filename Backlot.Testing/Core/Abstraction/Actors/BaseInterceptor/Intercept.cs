using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Actors.BaseInterceptor;

/// <summary>
/// Test to check if intercepting using the backlot interceptors works correctly.
/// </summary>
public class Intercept
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    // TODO: Move alias-specific test initialization to a shared helper
    // in the planned Backlot.Testing.Defaults NuGet package.
    // Also review how to avoid duplicate Initialize.Setup() calls.
    private static void AliasSetup()
    {
        Initialize.Setup((builder) => new AliasDirector(new LocalDiskStorage(), Substitute.For<IConfigurationManager>(), builder));
    }

    [Test]
    public void Intercept_WithJSONActor_ActorAndProxiedValuesAreInSync()
    {
        // Arrange
        var origin = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });
        
        var role = origin.Presents<IFormula>();
        var jProxy = (IJProxy)role;

        var initialActorValue = jProxy.JActor["Number1"]?.Value<int?>();
        var initialRoleValue = role.Number1;

        // Act
        role.Number1 = 8;
        
        // because JSON origins do work as a cloned JSON container the original object is not updated, however the "cloned" Origin is.
        // with JSON objects the role and the "origin"/Actor are updated, the original object is not updated, because it is deepcloned
        // JSON objects do work "by 'value'".
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialActorValue, Is.EqualTo(7));
            Assert.That(initialRoleValue, Is.EqualTo(7));
        
            Assert.That(role.Number1, Is.EqualTo(8));
            Assert.That(jProxy.JActor["Number1"]?.Value<int?>(), Is.EqualTo(8)); 
            
            Assert.That(origin["Number1"]?.Value<int?>(), Is.Not.EqualTo(8), "JsonInterception does work with DeepCloned objects");
        });
    }
    
    [Test]
    public void Intercept_WithJSONNestedRoleACTOR_ActorAndProxiedValuesAreInSync()
    {
        // Arrange
        var origin = JObject.FromObject(new
        {
            Uid = "5DEB1636",
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
        var jProxy = (IJProxy)role;

        var initialActorValue = jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>();
        var initialRoleValue = role.Formulas.First().Number1;
        
        // Act
        role.Formulas.First().Number1 = 8;
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialActorValue, Is.EqualTo(7));
            Assert.That(initialRoleValue, Is.EqualTo(7));
            
            Assert.That(jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.EqualTo(8));
            Assert.That(role.Formulas.First().Number1, Is.EqualTo(8));
            
            Assert.That(origin["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.Not.EqualTo(8));
        });
    }
    
    [Test]
    public void Intercept_WithJSONNestedTypedACTOR_ActorAndProxiedValuesAreInSync()
    {
        // Arrange
        var origin = JObject.FromObject(new
        {
            Uid = "5DEB1636",
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
        var jProxy = (IJProxy)role;

        var initialActorValue = jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>();
        var initialRoleValue = role.Formulas.First().Number1;

        // Act
        role.Formulas.First().Number1 = 8;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialActorValue, Is.EqualTo(7));
            Assert.That(initialRoleValue, Is.EqualTo(7));
        
            Assert.That(jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.EqualTo(8));
            Assert.That(role.Formulas.First().Number1, Is.EqualTo(8));
            
            Assert.That(origin["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.Not.EqualTo(8));
        });
    }
    
    [Test]
    public void Intercept_WithJSONNestedSelfActor_ActorAndProxiedValuesAreInSync()
    {
        // Arrange
        var origin = JObject.FromObject(new
        {
            Uid = "5DEB1636",
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
        var jProxy = (IJProxy)role;

        var initialActorValue = jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>();
        var initialRoleValue = role.Formulas.First().Number1;

        // Act
        role.Formulas.First().Number1 = 8;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialActorValue, Is.EqualTo(7));
            Assert.That(initialRoleValue, Is.EqualTo(7));
        
            Assert.That(jProxy.JActor["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.EqualTo(8));
            Assert.That(role.Formulas.First().Number1, Is.EqualTo(8));
            
            Assert.That(origin["Formulas"]?[0]?["Number1"]?.Value<int?>(), Is.Not.EqualTo(8));
        });
    }
    
    [Test]
    public void Intercept_WithJSONActorUsingAliasses_AliasIsUpdatedInRoleAndDeepClonedOrigin()
    {
        // Arrange
        AliasSetup();
        
        var origin = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Op = "sum"
        });
        
        var role = origin.Presents<IFormula>();
        var jProxy = (IJProxy)role;

        // Act
        role.Operation = "new";
        role.Number3 = 99;
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Operation, Is.EqualTo("new"));
            Assert.That(jProxy.JActor["Op"]?.Value<string?>(), Is.EqualTo("new"));
            Assert.That(origin["Op"]?.Value<string?>(), Is.Not.EqualTo("new"), "JsonInterception does work with DeepCloned objects");
            Assert.That(role.Number3, Is.EqualTo(99));
        });
    }
}