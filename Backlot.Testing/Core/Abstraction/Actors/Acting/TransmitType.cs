using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Backlot.Testing.Core.Abstraction.Actors.Acting;

public class TransmitType
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void TransmitType_APersistableRoleTransmittedToANonePersistableRole_OnlyFieldsFromTheDestinationTypeAreUsedInTheResult()
    {
        // Arrange
        var initial = new Formula()
        {
            Uid = "023d02600aea",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        };
        
        var formula = initial.Presents<IFormula>();

        // Act
        // An example when you do not have access to the typeof(INumberBase) f.e.
        var destination = formula.TransmitType(Backlot.Core.Loader.GetRoleByName("NumberBase"));
        var actor = ((IProxiedRole)destination).Actor as IDictionary<string, object>;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((destination as INumberBase)?.Number1, Is.EqualTo(7));
            Assert.That((destination as INumberBase)?.Number2, Is.EqualTo(9));
            Assert.That(actor, Does.Not.ContainKey("Operation"));
        });
    }
}