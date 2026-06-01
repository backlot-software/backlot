using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing;

/// <summary>
/// Tests to test if checksum calculation for roles (and or merged roles) works correctly
/// Checksums can be used to check role object equality
/// </summary>
public class Checksum
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Checksum_SelfRoleWithThePropertyValues_ChecksumsAreBasedOnContentEquality()
    {
        //A self object is an object implementing a role interface itself. 
        
        #region ARRANGE
        
        var origin = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        }.Presents<IFormula>(); 
        
        var thesame = new FormulaSelf()
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


        #endregion
        
        #region ACT

        var chkOrigin = origin.GetChecksum();
        var chkMergedTheSame = thesame.GetChecksum();
        var chkNotTheSame = notTheSame.GetChecksum();
        
        #endregion

        Assert.That(chkOrigin == chkMergedTheSame);
        Assert.That(chkOrigin == chkNotTheSame, Is.False);
    }
    
    [Test]
    public void Checksum_MergedJsonActors_ChecksumsAreBasedOnContentEquality()
    {
        //A self object is an object implementing a role interface itself. 
        
        #region ARRANGE
        
        var jorigin = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        });
        
        var jthesame = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number2 = 10,
        });
        
        var jupdate = JObject.FromObject(new
        {
            Uid = "f5d3c5aa",
            Number2 = 9,
        });

        var origin = jorigin.Presents<IPersist>(); //works even when presenting as another role.
        var thesame = jthesame.Presents<IFormula>();
        var update = jupdate.Presents<IFormula>();

        #endregion
        
        #region ACT

        var chkOrigin = origin.GetChecksum();
        ((IProxiedRole)thesame).Interceptor.CombineActor(origin as IProxiedRole);
        var chkMergedTheSame = thesame.GetChecksum();
        ((IProxiedRole)update).Interceptor.CombineActor(origin as IProxiedRole);
        var chkUpdate = update.GetChecksum();
        
        #endregion

        Assert.That(chkOrigin == chkMergedTheSame);
        Assert.That(chkOrigin != chkUpdate);
    }
    
    [Test]
    public void Checksum_TypedActorRoleWithThePropertyValues_ChecksumsAreBasedOnContentEquality()
    {
        //A self object is an object implementing a role interface itself. 
        
        #region ARRANGE
        
        var forigin = new Formula
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var fthesame = new Formula
        {
            Uid = "f5d3c5aa",
            Number1 = 12,
            Number2 = 10,
            Operation = "sum"
        };
        
        var fnotthesame = new Formula
        {
            Uid = "f5d3c5aa",
            Number2 = 9,
        };

        var origin = forigin.Presents<IPersist>(); //works even when presenting as another role.
        var thesame = fthesame.Presents<IFormula>();
        var notTheSame = fnotthesame.Presents<IFormula>();

        #endregion

        #region ACT
        
        var chkOrigin = origin.GetChecksum();
        var chkMergedTheSame = thesame.GetChecksum();
        var chkNotTheSame = notTheSame.GetChecksum();
        
        #endregion

        Assert.That(chkOrigin == chkMergedTheSame);
        Assert.That(chkOrigin == chkNotTheSame, Is.False);
        
        //
        //var chkOrigin = origin.GetChecksum();
        //thesame.Merge(origin);
        //var chkMergedTheSame = thesame.GetChecksum();
        //update.Merge(origin);
        //var chkUpdate = update.GetChecksum();
        //
        //#endregion
        //
        //Assert.IsTrue(chkOrigin == chkMergedTheSame);
        //Assert.IsTrue(chkOrigin != chkUpdate);
    }
}