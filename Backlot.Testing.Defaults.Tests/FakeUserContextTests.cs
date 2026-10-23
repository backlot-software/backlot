using Backlot.Testing.Defaults.Fakes;

namespace Backlot.Testing.Defaults.Tests;

public class FakeUserContextTests
{
    [Test]
    public void IsInGroup_DefaultGroups_UsesDefaultGroups()
    {
        // Arrange
        var fakeUser = new FakeUserContext();

        // Act
        var isInEveryone = fakeUser.IsInGroup("Everyone");
        var isInAdmin = fakeUser.IsInGroup("Admin");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isInEveryone, Is.True);
            Assert.That(isInAdmin, Is.True);
        });
    }
    
    [Test]
    public void IsInGroup_CustomGroups_UsesConfiguredGroups()
    {
        // Arrange
        var fakeUser = new FakeUserContext
        {
            Groups = ["Manager", "Admin"]
        };

        // Act
        var isInManager = fakeUser.IsInGroup("Manager");
        var isInAdmin = fakeUser.IsInGroup("Admin");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isInManager, Is.True);
            Assert.That(isInAdmin, Is.True);
        });
    }

    [Test]
    public void FakeUserContext_DefaultContext_UsesExpectedValues()
    {
        // Arrange
        var fakeUser = new FakeUserContext();
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(fakeUser.UserName, Is.EqualTo("UnitTestUser"));
            Assert.That(fakeUser.AuthScheme, Is.EqualTo("Bearer"));
            Assert.That(fakeUser.IsAuthenticated, Is.True);
            Assert.That(fakeUser.Groups, Is.EquivalentTo(["Everyone", "Admin"]));
            Assert.That(fakeUser.Claims, Is.Empty);
        });

    }
}