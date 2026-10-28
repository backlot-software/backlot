# Backlot.Testing.Defaults

A reusable NuGet package that provides common testing infrastructure for Backlot projects. The package offers a simple default test environment. You can use the provided components or customize them for your own tests.

## 1. Components
Only globally reusable components belong in this package. Feature-specific test models, scenarios, and helpers remain in their respective testing projects.

| Component | Purpose |
|---|---|
| `FakeConfiguration` | Provides configuration values from an in-memory dictionary. |
| `FakeStorage` | Provides an in-memory file system. |
| `FakeUserContext` | Provides a configurable test user context. |
| `FakeDefaultDirector` | Provides a base director that can be extended. |
| `Initialize` | Creates and configures the Backlot test environment. |

## 2. Getting Started

The package uses Autofac to configure a test environment.

- `Initialize.BuildApp()` builds the container and configures the Backlot service locator.
- `Initialize.ConfigureBacklot()` registers default services and initializes the director.

### Default Setup

Use the default setup when no custom services or configuration are required.

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot());
```

This setup uses `FakeDefaultDirector`, `FakeUserContext`, `FakeStorage` and `FakeConfiguration`. It also registers logging and in-memory caching services by default.

No custom director is required.

## 3. Configuration and Customization

You can customize the default environment when your tests require different behavior. You can provide configuration values, add or override services and create your own Director.

Choose the appropriate customization method:
- **settings**: Provide configuration values.
- **configureContainer**: Add or override services for a specific test setup.
- **Custom Director**: Define feature-specific service registrations and initialization behavior.

These options can be combined.

### FakeConfiguration

Uses an in-memory dictionary to store and provide configuration values.

If your tests require custom configuration values, you can provide them in two ways:

**A. Default Setup - Supply custom configuration through `settings`**

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        settings: new Dictionary<string, string?>
        {
            [$"{typeof(Settings).FullName}.{nameof(Settings.SigningKey)}"] = "test-signing-key"
        }));
```

**B. Custom Director - Add custom configuration with `Update()`**
```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot((config, b) =>
    {
        _ = config.Update(new ConfigurationInfo
        {
            Name = $"{typeof(Settings).FullName}.{nameof(Settings.SigningKey)}",
            Value = "test-signing-key"
        });

        return new CustomTestDirector(
            new FakeStorage(), config, b);
    }));
```

Configuration keys use the full settings type name and property name. Missing settings return an empty string.

Some `IConfigurationManager` methods are not implemented. Use a custom `IConfigurationManager` implementation when other behavior is required.

### FakeUserContext

Simulates a user without performing authentication. `FakeDefaultDirector` registers it automatically as the default `IUserCOntext`.

The default user has these values:
- **Username**: `UnitTestUser`
- **Authentication scheme**: `Bearer`
- **Groups**: `Everyone`, `Admin`
- **Authenticated**: `true`
- **Claims**: Empty

When tests require a different user, configure a `FakeUserContext` and register it through `configureContainer`:

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        configureContainer: b =>
        {
            b.RegisterInstance(new FakeUserContext
            {
                UserName = "TestUser",
                Groups = ["Everyone", "Manager"],
                IsAuthenticated = true
            }).As<IUserContext>();
        }));
```
The configured instance replaces the default `IUserContext`.
For custom Directors, you can register your own `IUserContext` implementation in `Registration()`.

Use `FakeUserContext` for tests that require a predefined user. Use another `IUserContext` implementation when testing actual authentication behavior.

### Custom Director

Extend `FakeDefaultDirector` when a test needs feature-specific services or initialization behavior.

```csharp
public class CustomTestDirector(
    IFileSystem fileSystem,
    IConfigurationManager configuration,
    ContainerBuilder builder)
    : FakeDefaultDirector(fileSystem, configuration, builder)
{
    public override void Registration()
    {
        base.Registration();

        // Register feature-specific services here.
    }
}
```

Call `base.Registration()` to retain default director registrations.
Register feature-specific services in the custom Director's `Registration()` method.

**Initialize the custom Director**:

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        (config, b) => new CustomTestDirector(
            new FakeStorage(), config, b)));
```
**The custom Director receives**:

- **`IFileSystem`:** Handles file operations. `FakeStorage` is available for this purpose (used in the example), but you can provide another implementation.
- **`IConfigurationManager`:** Provides configuration values. `FakeConfiguration` is used by default.
- **`ContainerBuilder`:** Allows the Director to register services.

### Additional Services and Overrides

Use `configureContainer` for additional registrations or overrides:

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        configureContainer: b =>
        {
            b.RegisterType<TestService>().AsSelf();
        }));
```

`configureContainer` allows you to register additional services. In this example,`TestService` is a class defined in the consuming test project and is added to the container. Other default services remain available.

`configureContainer` can also **override** existing services by registering another implementation of the same interface.

**Registrations are applied in this order:**

1. Default logging and caching services.
2. Director registrations and initialization.
3. `configureContainer` registrations.

For custom Directors, prefer `director.Registration()` for feature-specific services. Use `configureContainer` for additional test-specific registrations or overrides.

## 4. Limitations

The package provides basic test implementations, not complete replacements for production services.

- Fake services do not necessarily reproduce production behavior.
- `FakeConfiguration` does not implement every IConfigurationManager operation.
- `FakeUserContext` does not perform real authentication.
- Repositories or project-specific test components are not provided by default.

Use a custom implementation when a test requires behavior that the package does not provide.

## 5. Maintaining and Extending

Only add components that are reusable across different Backlot testing projects.

When adding or changing a component:

1. Keep its default behavior simple and predictable.
2. Allow configuration when necessary.
3. Document its usage, behavior, and limitations.
4. Add tests for default behavior and potential customization.

### Building and Testing

Before publishing, verify the package functionality in the `Backlot.Testing.Defaults.tests` project. Confirm that default initialization, custom configuration, and service overrides work.

**Run the package tests:**

```bash
dotnet test Backlot.Testing.Defaults.Tests/Backlot.Testing.Defaults.Tests.csproj
```

**Build the NuGet package:**

```bash
dotnet pack Backlot.Testing.Defaults/Backlot.Testing.Defaults.csproj -c Release
```