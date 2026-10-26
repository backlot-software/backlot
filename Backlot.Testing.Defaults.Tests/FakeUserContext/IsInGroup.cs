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
}