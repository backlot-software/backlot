# Backlot.Testing.Defaults

A reusable NuGet package that provides common testing infrastructure for Backlot projects. The package offers a simple default test environment. Contributors can use the provided components or customize them for their own tests.

Only globally reusable components belong in this package. Feature-specific test models, scenarios, and helpers remain in their respective testing projects.

## 1. Components

| Component | Purpose |
|---|---|
| `FakeConfiguration` | Provides configuration values from an in-memory dictionary. |
| `FakeStorage` | Provides an in-memory file system. |
| `FakeUserContext` | Provides a configurable test user context. *(TODO)* |
| `FakeDefaultDirector` | Provides a base director that contributors can extend. |
| `Initialize` | Creates and configures the Backlot test environment. |

## 2. Getting Started

The package uses Autofac to configure a test environment.

- `Initialize.BuildApp()` creates the container and configures the service locator.
- `Initialize.ConfigureBacklot()` registers default services and initializes the director.

The package currently provides default logging and caching registrations.

*TODO: Add a short example of the final default setup.*

## 3. Customization (Planned/TODO)

Contributors can customize the default environment when their tests require different behavior.

- **Configuration:** Provide custom values through `FakeConfiguration`.
- **User context:** Configure `FakeUserContext` or provide another `IUserContext`.
- **Director:** Extend `FakeDefaultDirector` and override `Registration()` or `Incept()`.
- **Services:** Register custom services through Autofac.

Call `base.Registration()` when a custom director must retain the default director registrations.

*TODO: Add a short example of a custom director and service override.*

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