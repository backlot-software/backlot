using Backlot.Core.Security;
using Backlot.Testing.Shared.Stubs;
using NUnit.Framework;

namespace Backlot.Testing.Core.Security.ReadOnlyPermission;

public class ToString
{
    [Test]
    public void ToString_GroupHasHigherLevelThanMask_ReturnsExpectedPattern()
    {
        // Arrange
        var permission = Permission.Create(PermissionLevel.None)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Open, PermissionLevel.ReadExecute);
        
        // Act
        var result = permission.ToString();
        
        // Assert
        Assert.That(result, Is.EqualTo("m::0,g:*:5,g:Admin:7"));
    }
    
    [Test]
    public void ToString_WildcardHasHigherLevelThanGroup_ReturnsExpectedPattern()
    {
        // Arrange
        var permission = Permission.Create(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.None)
            .SetGroup(Access.Open, PermissionLevel.ReadExecute);
        
        // Act
        var result = permission.ToString();
        
        // Assert
        Assert.That(result, Is.EqualTo("m::7,g:*:5,g:Admin:0"));
    }
    
    [Test]
    public void ToString_TwoGroupsWithDifferentLevels_ReturnsExpectedPattern()
    {
        // Arrange
        var permission = Permission.Create(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Everyone, PermissionLevel.ReadExecute);
        
        // Act
        var result = permission.ToString();
        
        // Assert
        Assert.That(result, Is.EqualTo("m::7,g:Admin:7,g:Everyone:5"));
    }
    
    [Test]
    public void ToString_ExplicitUserPermissionSet_ReturnsExpectedPattern()
    {
        // Arrange
        var permission = Permission.Create(PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Open, PermissionLevel.None)
            .SetUser(UserCtx.UserNameStatic, PermissionLevel.Read);
        
        // Act
        var result = permission.ToString();
        
        // Assert
        Assert.That(result, Is.EqualTo($"m::7,g:*:0,g:Admin:7,u:{UserCtx.UserNameStatic}:4"));
    }
}