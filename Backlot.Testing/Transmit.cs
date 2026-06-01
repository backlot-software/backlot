using System;
using System.Collections.Generic;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Testing.Core;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing;

/// <summary>
/// Tests on presenting a role from a source object or loaded from a database.
/// </summary>
public class Transmit
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void Transmit_APersistedRoleTransmittedToANonePersistedRole_OnlyFieldsFromTheDestinationTypeAreUsedInTheResult()
    {
        #region ARRANGE

        var uid = "023d02600ded";

        var initial = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        #endregion

        #region ACT
        
        var formula = initial.Presents<IFormula>();
        var dest = formula.Transmit<IFormula, INumberBase>();
        var actor = ((IProxiedRole)dest).Actor as IDictionary<string, object>;

        #endregion
        
        // check dest.number1 == 7 and number 2 == 9
        Assert.That(dest.Number1 == 7);
        Assert.That(dest.Number2 == 9);
        
        // check proxy actor does not have an Operation key
        Assert.That(actor?.Keys.Contains("Operation"), Is.False);
    }
    
    [Test]
    public void TransmitType_APersistedRoleTransmittedToANonePersistedRole_OnlyFieldsFromTheDestinationTypeAreUsedInTheResult()
    {
        #region ARRANGE

        var uid = "023d02600aea";

        var initial = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        #endregion

        #region ACT
        
        var formula = initial.Presents<IFormula>();
        // An example when you do not have access to the typeof(INumberBase) f.e.
        var dest = formula.TransmitType(Loader.GetRoleByName("NumberBase"));
        var actor = ((IProxiedRole)dest).Actor as IDictionary<string, object>;

        #endregion
        
        // check dest.number1 == 7 and number 2 == 9
        Assert.That((dest as INumberBase)?.Number1 == 7);
        Assert.That((dest as INumberBase)?.Number2 == 9);
        
        // check proxy actor does not have an Operation key
        Assert.That(actor?.Keys.Contains("Operation"), Is.False);
    }
    
    
    [Test]
    public void Transmit_APersistedRoleTransmittedToAPersistedRole_ArgumentExceptionIsThrown()
    {
        #region ARRANGE

        var uid = "023d02600ded";

        var initial = new Formula()
        {
            Uid = uid,
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        #endregion

        #region ACT
        
        var formula = initial.Presents<IFormula>();
        
        #endregion
        
        // an ArgumentException is thrown while transmist
        Assert.Throws<ArgumentException>(() =>
        {
            formula.Transmit<IFormula, IFormula>();
        });
    }
}