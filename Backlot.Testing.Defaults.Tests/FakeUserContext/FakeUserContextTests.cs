namespace Backlot.Testing.Defaults.Tests.FakeUserContext;

public class FakeUserContextTests
{
    [Test]
    public void FakeUserContext_DefaultContext_UsesExpectedValues()
    {
        // Arrange
        var fakeUser = new Fakes.FakeUserContext();
        
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