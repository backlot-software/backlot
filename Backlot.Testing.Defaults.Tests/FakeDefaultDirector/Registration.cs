using Autofac;
using Backlot.Core.Security;
using Backlot.Testing.Defaults.Fakes;

namespace Backlot.Testing.Defaults.Tests.FakeDefaultDirector;

public class Registration
{
    [Test]
    public void Registration_DefaultDirector_RegistersFakeUserContext()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var storage = new FakeStorage();
        var configuration = new FakeConfiguration(
            new Dictionary<string, string?>());

        var director = new Fakes.FakeDefaultDirector(
            storage, configuration, builder);

        // Act
        director.Registration();

        var container = builder.Build();
        var userContext = container.Resolve<IUserContext>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(userContext, Is.InstanceOf<Fakes.FakeUserContext>());
            Assert.That(userContext.UserName, Is.EqualTo("UnitTestUser"));
        });
    }
}