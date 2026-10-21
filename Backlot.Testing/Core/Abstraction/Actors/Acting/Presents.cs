using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Actors.Acting;

public class Presents
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
    public void Presents_WithActorNotHavingAUidAndARoleAliasReferringToAnExpressionEngine_UidIsCalculatedBasedOnExpressionEngine()
    {
        // Arrange
        AliasSetup();
        
        var actor = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Name = "sumexample",
            FormulaId = "12",
        });

        // Act
        var role = actor.Presents<IFormula>();
        
        // Assert
        Assert.That(role.Uid, Is.EqualTo("12_sumexample!"));
    }
    
    [Test]
    public void Presents_OneActorPresentedTwiceAsDifferentRoles_ActsAsAnIJProxy()
    {
        //NOTE: casting back from a none proxied role is not possible (except when it is a self)
        //NOTE: However when proxied, a role has it's actor inside which can be casted to the original, see; Intercepting_WithTypedActor_ActorAndProxiedValuesAreInSync

        // Arrange
        var initial = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        // Act
        var formula = initial.Presents<IFormula>();
        var persisted = initial.Presents<IPersist>();

        // Assert
        Assert.Multiple(() =>
        {
            // Always handled with a JsonInterceptor and therefor a IJProxy/
            Assert.That(formula, Is.InstanceOf<IJProxy>());
            Assert.That(persisted, Is.InstanceOf<IJProxy>());

            //when presenting the role, the role properties are accessible.
            Assert.That(formula.Number1, Is.EqualTo(7));
            Assert.That(persisted.Uid, Is.EqualTo(formula.Uid));
        });
    }
    
    [Test]
    public void Presents_DictionaryActorWithNoneMatchingUid_NotMatchingUidIsResolvedFromAlias()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            { "BSN AN", "12355" },
            { "Name", "Jeroen" }
        };
        
        // Act
        var person = dictionary.Presents<IPerson>();
        
        // Assert
        Assert.That(person.Uid, Is.EqualTo("12355"));
    }
    
    [Test]
    public void Presents_TypedActorWithDifferentPropertyTypes_DifferentTypesAreConvertedToWhatTheRoleDefines()
    {
        // Arrange
        var origin = new Formula()
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        };

        // Act
        var role = origin.Presents<IFormulaTypeChanged>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Number1, Is.EqualTo("7"));
            Assert.That(role.Number2, Is.EqualTo("9"));
        });
    }
    
    [Test]
    public void Presents_AnonymousActor_ReturnsProxyWithAccessibleProperties()
    {
        // Arrange
        var initial = new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        // Act
        var formula = initial.Presents<IFormula>();

        // Assert
        Assert.Multiple(() =>
        {
            //the proxies are not of the same type as the actor, they present the role;
            Assert.That(formula is IProxiedRole);

            //the underlying actor is of the original type.
            Assert.That(((IProxiedRole)formula).Actor, Is.Not.Null);

            //when presenting the role, the role properties are accessible.
            Assert.That(formula.Number1, Is.EqualTo(7));
        });
    }
    
    [Test]
    public void Presents_WithJSONObjectsGettingUidFromDifferentSources_WhenUidIsFilledUidIsPreserved()
    {
        // Arrange
        // When created from the UI an item is created a custom generated Uid is used (no connection with the remote system yet).
        var newItem = JObject.FromObject(new
        {
            Uid = "8a89ca8f03db4f80a846d2e0d4b5d3cb",
            CustomerName = "Ferrari"
        });

        // Act
        var role = newItem.Presents<ICard>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Uid, Is.EqualTo("8a89ca8f03db4f80a846d2e0d4b5d3cb"));
            Assert.That(role.CardCode, Is.Null);
        });
    }
    
    [Test]
    public void Presents_WithJSONObjectsGettingUidFromDifferentSources_WhenCardCodeIsFilledUidIsCardCode()
    {
        // Arrange
        // When returned from the remote api the unique identifier of the external system is returned (CardCode in this case).
        var existingItem = JObject.FromObject(new
        {
            CardCode = "C001",
            CustomerName = "Ferrari"
        });

        // Act
        var role = existingItem.Presents<ICard>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role.Uid, Is.EqualTo("C001"));
            Assert.That(role.CardCode, Is.EqualTo(role.Uid));
        });
    }
    
    [Test]
    public void Presents_AnonymousActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        // Arrange
        AliasSetup();
        
        var origin = new
        {
            Operatie = "sum",
        };
        
        // Act
        var role = origin.Presents<IFormula>();

        // Assert
        Assert.That(role.Operation, Is.EqualTo("sum"));
    }

    [Test]
    public void
        Presents_DictionaryActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        // Arrange
        AliasSetup();
        
        var origin = new Dictionary<string, object>
        {
            ["Operatie"] = "sum"
        };

        // Act
        var role = origin.Presents<IFormula>();

        // Assert
        Assert.That(role.Operation, Is.EqualTo("sum"));
    }

    [Test]
    public void Presents_ExpandoObjectActorWithNotDirectMatchingPropertyName_NotDirectMatchingPropertyNameIsCorrectedByAlias()
    {
        AliasSetup();
        // Arrange
        AliasSetup();
        
        dynamic origin = new ExpandoObject();

        origin.Operatie = "sum";
        
        // Act
        var role = Backlot.Core.Abstraction.Actors.Acting.Presents<IFormula>(origin);

        // Assert
        Assert.That(role.Operation, Is.EqualTo("sum"));
    }
    
    [Test]
    public void Presents_WithJSONNestedRoleActorWhichNeedsAliassing_AliasValueIsUsedForRoleProperty()
    {
        // Arrange
        AliasSetup();
        
        var origin = JObject.FromObject(new
        {
            Uid = "5DEB1636",
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
        
        // Act
        var role = origin.Presents<IFormulaGroup>();

        // Assert
        Assert.That(role.Formulas.First().Operation, Is.EqualTo("sum")); // "Op" alias is used for "Operation" value.
    }
    
    [Test]
    public void Presents_TypedObjectWithItsOwnPermissionProperty_PermissionPropertyIsIgnoredAndDefaultIsUsed()
    {
        // Arrange
        var typed = new FormulaPermission
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            __Permission = "m::0" //try to overwrite backlot permissions
        };
        
        // Act
        var role = typed.Presents<IFormula>();

        // Assert
        Assert.Multiple(() =>
        {
            //because permission is not set via backlot, the default permission is returned. The Actor Interceptor does protect reading the origin permission and turns the actor value into null
            Assert.That(role.Permission(), Is.EqualTo(Permission.Create(PermissionLevel.ReadWriteExecute)), $"{nameof(PermissionLevel.ReadWriteExecute)} is default for IPermissionized roles.");
            Assert.That((role as IJProxy)?.JActor["__Permission"], Is.Null, $"Actor.{nameof(IPermission.__Permission)} is removed.");
        });
    }
    
    [Test]
    public void Presents_AnonymousObjectWithItsOwnPermissionProperty_PermissionPropertyIsIgnoredAndDefaultIsUsed()
    {
        // Arrange
        var anonymousObject = new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            __Permission = "m::0" //try to overwrite backlot permissions
        };
        
        // Act
        var role = anonymousObject.Presents<IFormula>();
        var per = (role as IJProxy)?.JActor[nameof(IPermission.__Permission)];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(role, Is.InstanceOf<IJProxy>()); // when presented an anonymous object is turned into a IJProxy.
            Assert.That(per, Is.Null); // permission value used in the "converted json" origin.
            Assert.That(role.Permission().ToString(), Is.EqualTo("m::7")); // default permission is set.
        });
    }
    
}