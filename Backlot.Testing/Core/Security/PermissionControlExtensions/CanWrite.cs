using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Security;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.PermissionControlExtensions;

public class CanWrite
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void CanWrite_CurrentUserWithAdminReadWritePermission_CurrentUserCanWrite()
    {
        // Arrange
        var typed = Acting.New<IFormula>();
        typed.Number1 = 7;
        typed.Number2 = 9;
        typed.Operation = "sum";
        
        typed.ManagePermission(p => p
            .SetMask(PermissionLevel.ReadWriteExecute)
            .Clear() // clear all other permissions and 
            .SetGroup(Access.Open, PermissionLevel.Read)
            .SetUser("TEST", PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWrite));
        
        // Act
        var result = typed.CanWrite();

        // Assert
        Assert.That(result,  Is.True);
    }
}