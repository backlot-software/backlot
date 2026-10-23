# Backlot.Testing.Defaults

A reusable NuGet package that provides common testing infrastructure for Backlot projects. The package offers a simple default test environment. Contributors can use the provided components or customize them for their own tests.

Only globally reusable components belong in this package. Feature-specific test models, scenarios, and helpers remain in their respective testing projects.

## 1. Components

| Component | Purpose |
|---|---|
| `FakeConfiguration` | Provides configuration values from an in-memory dictionary. |
| `FakeStorage` | Provides an in-memory file system. |
| `FakeUserContext` | Provides a configurable test user context. |
| `FakeDefaultDirector` | Provides a base director that contributors can extend. |
| `Initialize` | Creates and configures the Backlot test environment. |

## 2. Getting Started

The package uses Autofac to configure a test environment.

- `Initialize.BuildApp()` creates the container and configures the service locator.
- `Initialize.ConfigureBacklot()` registers default services and initializes the director.

The package currently provides default logging and caching registrations.

*TODO: Add a short example of the final default setup.*

## 3. Configuration and Customization (Planned/TODO)

Contributors can customize the default environment when their tests require different behavior.

- [**Configuration:**](#fakeconfiguration) Provide custom values through `FakeConfiguration`.
- [**User context:**](#fakeusercontext) Configure `FakeUserContext` or provide another `IUserContext`.
- [**Director:**](#custom-director-and-services) Extend `FakeDefaultDirector` and override `Registration()` or `Incept()`.
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

Contributors can override these values:

```csharp
var user = new FakeUserContext
{
    UserName = "TestUser",
    Groups = ["Everyone", "Manager"],
    IsAuthenticated = true
};
```

Use `FakeUserContext` for tests that require a predefined user. Use another `IUserContext`, such as `BuiltInUserContext`, when testing actual authentication behavior.

### Custom Director and Services

Extend `FakeDefaultDirector` to register feature-specific services or configure Backlot behavior.

Call `base.Registration()` to retain default director registrations.

*TODO: Add a custom director and service override example.*


## 4. Limitations

The package provides basic test implementations, not complete replacements for production services.

- Fake services do not necessarily reproduce production behavior.
- `FakeUserContext` will not perform real authentication.
- Project-specific test components are not included.

Use a custom implementation when a test requires behavior that the package does not provide.

## 5. Maintaining and Extending

Only add components that are reusable across different Backlot testing projects.

When adding or changing a component:

1. Keep its default behavior simple and predictable.
2. Allow configuration when necessary.
3. Document its usage, behavior, and limitations.
4. Validate the component before releasing a new package version.

*TODO: Add package build and release instructions.*