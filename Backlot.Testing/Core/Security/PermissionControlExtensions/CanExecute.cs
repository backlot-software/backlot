using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.PermissionControlExtensions;

public class CanExecute
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
    public void CanExecute_CurrentUserWithAdminReadWritePermission_CurrentUserCanNotExecute()
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
        var result = typed.CanExecute();

        // Assert
        Assert.That(result,  Is.False);
    }
}