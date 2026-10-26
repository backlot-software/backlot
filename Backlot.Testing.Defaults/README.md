# Backlot.Testing.Defaults

A reusable NuGet package that provides common testing infrastructure for Backlot projects. The package offers a simple default test environment. You can use the provided components or customize them for your own tests.

## 1. Components
Only globally reusable components belong in this package. Feature-specific test models, scenarios, and helpers remain in their respective testing projects.

| Component | Purpose |
|---|---|
| [`FakeConfiguration`](#fakeconfiguration) | Provides configuration values from an in-memory dictionary. |
| `FakeStorage` | Provides an in-memory file system. |
| [`FakeUserContext`](#fakeusercontext) | Provides a configurable test user context. |
| `FakeDefaultDirector` | Provides a base director that can be extend. |
| `Initialize` | Creates and configures the Backlot test environment. |

## 2. Getting Started

The package uses Autofac to configure a test environment.

- `Initialize.BuildApp()` builds the container and configures the Backlot service locator.
- `Initialize.ConfigureBacklot()` registers default services and initializes the director.

The package currently provides default logging and caching registrations.

### Default Setup

Use the default setup when no custom services or configuration are required.

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot());
```

This setup uses `FakeDefaultDirector`, `FakeUserContext`, `FakeStorage` and `FakeConfiguration`. It also registers default logging and in-memory caching services.

No custom director is required.

## 3. Configuration and Customization

You can customize the default environment when your tests require different behavior. You can provide configuration values, override services or create your own Director.

- [**Configuration:**](#fakeconfiguration) Provide custom values through `FakeConfiguration`.
- [**User context:**](#fakeusercontext) Configure `FakeUserContext` or provide another `IUserContext`.
- [**Director:**](#custom-director-and-services) Extend `FakeDefaultDirector` and override `Registration()` and/or `Incept()`.
- [**Services:**](#custom-director-and-services) Register custom services through Autofac.

### FakeConfiguration

Uses an in-memory dictionary to provide configuration values.

```csharp
var config = new FakeConfiguration(
    new Dictionary<string, string?>
    {
        ["MyFeature.Settings.Enabled"] = "true"
    });
```

Missing settings return an empty string. `Update()` adds new settings.

Some `IConfigurationManager` methods are not implemented. Use a custom `IConfigurationManager` when other behavior is required.

### FakeUserContext

Simulates a user without performing authentication.

Default values:
- Username: `UnitTestUser`
- Authentication scheme: `Bearer`
- Groups: `Everyone`, `Admin`
- Authenticated: `true`
- Claims: Empty

You can override and configure these values:

```csharp
var user = new FakeUserContext
{
    UserName = "TestUser",
    Groups = ["Everyone", "Manager"],
    IsAuthenticated = true
};
```

Use `FakeUserContext` for tests that require a predefined user. Use another `IUserContext` when testing actual authentication behavior.

### Custom Director

Extend `FakeDefaultDirector` to register feature-specific services or configure Backlot behavior.

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

- **`IFileSystem`:** Handles file operations. The example uses `FakeStorage`, but you can provide another implementation.
- **`IConfigurationManager`:** Provides configuration values. `FakeConfiguration` is used by default.
- **`ContainerBuilder`:** Allows the Director to register services.

### Service Overrides

Use `configureContainer` for additional registrations or overrides:

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        configureContainer: b =>
        {
            b.RegisterInstance(new FakeUserContext
            {
                UserName = "Manager"
            }).As<IUserContext>();
        }));
```

Default logging and caching services are always registered.

Registrations are applied in this order:

1. Default logging and caching services.
2. Director registrations and initialization.
3. Additional `configureContainer` registrations.

For custom Directors, prefer `director.Registration()` for feature-specific services. Use `configureContainer` for additional test-specific overrides.

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