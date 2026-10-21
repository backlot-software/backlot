using System;
using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Actors.Acting;

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
    public void Transmit_APersistableRoleTransmittedToANonePersistableRole_OnlyFieldsFromTheDestinationTypeAreUsedInTheResult()
    {
        // Arrange
        var initial = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };
        
        var formula = initial.Presents<IFormula>();

        // Act
        var destination = formula.Transmit<IFormula, INumberBase>();
        var actor = ((IProxiedRole)destination).Actor as IDictionary<string, object>;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(destination.Number1, Is.EqualTo(7));
            Assert.That(destination.Number2, Is.EqualTo(9));
            Assert.That(actor, Does.Not.ContainKey("Operation"));
        });
        
    }
    
    [Test]
    public void Transmit_APersistableRoleTransmittedToAPersistableRole_ThrowsArgumentException()
    {
        // Arrange
        var initial = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };

        var formula = initial.Presents<IFormula>();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            formula.Transmit<IFormula, IFormula>();
        });
    }
}