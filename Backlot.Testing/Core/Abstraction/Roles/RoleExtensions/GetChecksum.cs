using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Roles.RoleExtensions;

/// <summary>
/// Tests to test if checksum calculation for roles (and or merged roles) works correctly
/// Checksums can be used to check role object equality
/// </summary>
public class GetChecksum
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void GetChecksum_SelfRolesWithTheSamePropertyValues_ReturnsSameChecksum()
    {
        //A self object is an object implementing a role interface itself. 
        
        // Arrange
        var origin = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        }.Presents<IFormula>(); 
        
        var theSame = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        }.Presents<IFormula>(); 
        
        // Act
        var chkOrigin = origin.GetChecksum();
        var chkMergedTheSame = theSame.GetChecksum();

        // Assert
        Assert.That(chkOrigin, Is.EqualTo(chkMergedTheSame));
    }
    
    [Test]
    public void GetChecksum_SelfRolesWithDifferentPropertyValues_ReturnsDifferentChecksum()
    {
        //A self object is an object implementing a role interface itself. 
        
        // Arrange
        var origin = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        }.Presents<IFormula>(); 
        
        var notTheSame = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number2 = 9,
        }.Presents<IFormula>(); 
        
        // Act
        var chkOrigin = origin.GetChecksum();
        var chkNotTheSame = notTheSame.GetChecksum();

        // Assert
        Assert.That(chkOrigin,  Is.Not.EqualTo(chkNotTheSame));
    }
    
    [Test]
    public void GetChecksum_MergedJsonActorsWithTheSamePropertyValues_ReturnSameChecksum()
    {
        // Arrange
        var jOrigin = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        });
        
        var jTheSame = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number2 = 10,
        });

        var origin = jOrigin.Presents<IPersist>(); //works even when presenting as another role.
        var theSame = jTheSame.Presents<IFormula>();
        
        // Act
        var chkOrigin = origin.GetChecksum();
        ((IProxiedRole)theSame).Interceptor.CombineActor(origin as IProxiedRole);
        var chkMergedTheSame = theSame.GetChecksum();

        // Assert
        Assert.That(chkOrigin, Is.EqualTo(chkMergedTheSame));
    }
    
    [Test]
    public void GetChecksum_MergedJsonActorsWithDifferentPropertyValues_ReturnsDifferentChecksum()
    {
        // Arrange
        var jOrigin = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        });
        
        var jUpdate = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number2 = 9,
        });

        var origin = jOrigin.Presents<IPersist>(); //works even when presenting as another role.
        var update = jUpdate.Presents<IFormula>();
        
        // Act
        var chkOrigin = origin.GetChecksum();
        ((IProxiedRole)update).Interceptor.CombineActor(origin as IProxiedRole);
        var chkUpdate = update.GetChecksum();

        // Assert
        Assert.That(chkOrigin, Is.Not.EqualTo(chkUpdate));
    }
    
    [Test]
    public void GetChecksum_TypedActorRoleWithTheSamePropertyValues_ReturnsSameChecksum()
    {
        // Arrange
        var fOrigin = new Formula
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var fTheSame = new Formula
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };

        var origin = fOrigin.Presents<IPersist>(); //works even when presenting as another role.
        var theSame = fTheSame.Presents<IFormula>();

        // Act
        var chkOrigin = origin.GetChecksum();
        var chkMergedTheSame = theSame.GetChecksum();

        // Assert
        Assert.That(chkOrigin, Is.EqualTo(chkMergedTheSame));
    }
    
    [Test]
    public void GetChecksum_TypedActorRoleWithDifferentPropertyValues_ReturnsDifferentChecksum()
    {
        // Arrange
        var forigin = new Formula
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var fNotTheSame = new Formula
        {
            Uid = "f5d3c5aa",
            Number2 = 9,
        };

        var origin = forigin.Presents<IPersist>(); //works even when presenting as another role.
        var notTheSame = fNotTheSame.Presents<IFormula>();

        //Act
        var chkOrigin = origin.GetChecksum();
        var chkNotTheSame = notTheSame.GetChecksum();
        
        // Assert
        Assert.That(chkOrigin, Is.Not.EqualTo(chkNotTheSame));
    }
}