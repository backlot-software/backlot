using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.PermissionControlExtensions;

public class ManagePermission
{
    #region Backlot Test Environment Setup

    [SetUp]
    public void Setup()
    {
        Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b)));
    }

    #endregion
    
    /// <summary>
    /// Verifies changing the permission mask updates the permission of a presented anonymous actor.
    /// </summary>
    [Test]
    public void ManagePermission_AnonymousObjectWithDefaultPermission_PermissionIsEqualToChangedMaskLevel()
    {
        // Arrange
        var anonymousObject = new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        };
        
        var role = anonymousObject.Presents<IFormula>();
        
        // Act
        role.ManagePermission(p => p.SetMask(PermissionLevel.ReadWrite));
        
        // Assert
        Assert.That(role.Permission(), Is.EqualTo(Permission.Create(PermissionLevel.ReadWrite)));
    }
}