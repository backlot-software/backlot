using Backlot.Core;
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
    [SetUp]
    public void Setup()
    {
        Defaults.Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b)));
    }
    
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