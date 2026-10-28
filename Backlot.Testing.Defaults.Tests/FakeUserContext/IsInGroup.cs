namespace Backlot.Testing.Defaults.Tests.FakeUserContext;

public class IsInGroup
{
    [Test]
    public void IsInGroup_DefaultGroups_UsesDefaultGroups()
    {
        // Arrange
        var fakeUser = new Fakes.FakeUserContext();

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
        var fakeUser = new Fakes.FakeUserContext
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
    public void IsInGroup_DifferentCase_IsCaseInsensitiveAndReturnsTrue()
    {
        // Arrange
        var fakeUser = new Fakes.FakeUserContext
        {
            Groups = ["Manager", "Admin"]
        };

        // Act
        var isInManager = fakeUser.IsInGroup("manager");
        var isInAdmin = fakeUser.IsInGroup("admin");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isInManager, Is.True);
            Assert.That(isInAdmin, Is.True);
        });
    }
    
    [Test]
    public void IsInGroup_MissingGroup_ReturnsFalse()
    {
        // Arrange
        var fakeUser = new Fakes.FakeUserContext
        {
            Groups = ["Manager", "Admin"]
        };

        // Act
        var result = fakeUser.IsInGroup("MissingGroup");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
        });
    }
    
    [Test]
    public void IsInGroup_EmptyGroups_ReturnsFalse()
    {
        // Arrange
        var fakeUser = new Fakes.FakeUserContext
        {
            Groups = []
        };

        // Act
        var isInManager = fakeUser.IsInGroup("manager");
        var isInAdmin = fakeUser.IsInGroup("admin");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isInManager, Is.False);
            Assert.That(isInAdmin, Is.False);
        });
    }
}