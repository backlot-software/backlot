using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Security;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.PermissionControlExtensions;

public class CurrentUserPermissionLevel
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
    public void CurrentUserPermissionLevel_GroupHasHigherLevelThanMask_ReturnsMaskPermissionLevel()
    {
        // Arrange
        var role = Acting.New<IFormula>();
        role.Number1 = 7;
        role.Number2 = 9;
        role.Operation = "sum";
        
        
        role.ManagePermission(p => p
            .SetMask(PermissionLevel.None)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Open, PermissionLevel.ReadExecute));     

        // Act
        var result = role.CurrentUserPermissionLevel();

        // Assert
        Assert.That(result, Is.EqualTo(PermissionLevel.None));
    }
    
    [Test]
    public void CurrentUserPermissionLevel_WildcardHasHigherLevelThanGroup_ReturnsWildcardPermissionLevel()
    {
        // Arrange
        var role = Acting.New<IFormula>();
        role.Number1 = 7;
        role.Number2 = 9;
        role.Operation = "sum";
        
        
        role.ManagePermission(p => p
            .SetMask(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.None)
            .SetGroup(Access.Open, PermissionLevel.ReadExecute)); //highest group level  

        // Act
        var result = role.CurrentUserPermissionLevel();

        // Assert
        Assert.That(result, Is.EqualTo(PermissionLevel.ReadExecute)); //result of the highest group level (thus the wildcard) is used.
    }
    
    [Test]
    public void CurrentUserPermissionLevel_TwoGroupsWithDifferentLevels_ReturnsHighestGroupPermissionLevel()
    {
        // Arrange
        var role = Acting.New<IFormula>();
        role.Number1 = 7;
        role.Number2 = 9;
        role.Operation = "sum";
        
        
        role.ManagePermission(p => p
            .SetMask(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute) //highest group level
            .SetGroup(Access.Everyone, PermissionLevel.ReadExecute));  

        // Act
        var result = role.CurrentUserPermissionLevel();

        // Assert
        Assert.That(result, Is.EqualTo(PermissionLevel.ReadWriteExecute)); //result of the highest group level is used; admin in this case.
    }
    
    [Test]
    public void CurrentUserPermissionLevel_ExplicitUserPermissionSet_ReturnsUserPermissionLevel()
    {
        // Arrange
        var role = Acting.New<IFormula>();
        role.Number1 = 7;
        role.Number2 = 9;
        role.Operation = "sum";


        role.ManagePermission(p => p
            .SetMask(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Open, PermissionLevel.None)
            .SetUser(UserContext.Current.UserName, PermissionLevel.Read));

        // Act
        var result = role.CurrentUserPermissionLevel();

        // Assert
        Assert.That(result, Is.EqualTo(PermissionLevel.Read));
    }
}