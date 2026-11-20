using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.PermissionControlExtensions;

public class CanRead
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
    /// Verifies a user without matching user or group read permissions cannot read.
    /// </summary>
    [Test]
    public void CanRead_CurrentUserWithoutMatchingPermissions_CurrentUserCanNotRead()
    {
        // Arrange
        var typed = Acting.New<IFormula>();
        typed.Number1 = 7;
        typed.Number2 = 9;
        typed.Operation = "sum";

        typed.ManagePermission(p =>
            p.SetUser("SomeOtherUser", PermissionLevel.ReadExecute)
                .SetGroup("SuperAccess", PermissionLevel.ReadWriteExecute));
        
        // Act
        var canRead = typed.CanRead();
        
        // Assert
        Assert.That(canRead, Is.False);
    }
}