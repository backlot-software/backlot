using System;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Loader;

public class TryGetRoleByName
{
    /// <summary>
    /// Verifies valid role names resolve case-insensitively, with or without the leading I.
    /// </summary>
    [TestCase ("InterfaceStartingWithI", typeof(IInterfaceStartingWithI))]
    [TestCase ("interfacestartingwithi", typeof(IInterfaceStartingWithI))]
    [TestCase ("INTERFACESTARTINGWITHI", typeof(IInterfaceStartingWithI))]
    [TestCase ("IInterfaceStartingWithI", typeof(IInterfaceStartingWithI))]
    [TestCase("Formula", typeof(IFormula))]
    [TestCase("formula", typeof(IFormula))]
    [TestCase("FORMULA", typeof(IFormula))]
    [TestCase("IFormula", typeof(IFormula))]
    public void TryGetRoleByName_ValidRoleName_ReturnsTrueAndExpectedType(string roleName, Type expectedType)
    {
        // Act
        var correctTypeFound = Backlot.Core.Loader.TryGetRoleByName(roleName, out var actualType);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(correctTypeFound, Is.True);
            Assert.That(actualType, Is.EqualTo(expectedType));
        });
    }
    
    /// <summary>
    /// Verifies unknown or non-role names return false and a null type.
    /// </summary>
    [TestCase ("RoleThatDoesNotExist")]
    [TestCase ("nterfaceStartingWithI")]
    [TestCase ("FormulaSelf")]
    
    public void TryGetRoleByName_UnknownRoleName_ReturnsFalseAndNullType(string roleName)
    {
        // Act
        var correctTypeFound = Backlot.Core.Loader.TryGetRoleByName(roleName, out var actualType);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(correctTypeFound, Is.False);
            Assert.That(actualType, Is.Null);
        });
    }
}