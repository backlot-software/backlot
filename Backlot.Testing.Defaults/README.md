# Backlot.Testing.Defaults

A reusable NuGet package that provides common testing infrastructure for Backlot projects. The package offers a simple default test environment. You can use the provided components or customize them for your own tests.

Only globally reusable components belong in this package. Feature-specific test models, scenarios, and helpers remain in their respective testing projects.

## 1. Getting Started

The package uses Autofac to configure a test environment.

- `Initialize.BuildApp()` builds the container and configures the Backlot service locator.
- `Initialize.ConfigureBacklot()` registers default services and initializes the director.

### Default Setup

Use the default setup when no custom services or configuration are required.

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot());
```

## 2. Configuration and Customization

You can customize the default environment when your tests require different behavior. You can provide configuration values, add or override services and create your own Director.

Choose the appropriate customization method:
- **settings**: Provide configuration values.
- **configureContainer**: Add or override services for a specific test setup.
- **Custom Director**: Define feature-specific service registrations and initialization behavior.

These options can be combined.

### settings

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

### configureContainer

When test setup requires extra or modified services, register them through `configureContainer`.

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        configureContainer: b =>
        {
            b.RegisterType<TestService>().AsSelf();
        }));
```

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

**Initialize the custom Director**:

```csharp
Initialize.BuildApp(builder =>
    builder.ConfigureBacklot(
        (config, b) => new CustomTestDirector(
            new FakeStorage(), config, b)));
```

## 3. Limitations

The package provides basic test implementations, not complete replacements for production services.

- Fake services do not necessarily reproduce production behavior.
- Repositories or project-specific test components are not provided by default.

## 4. Maintaining and Extending

Only add components that are reusable across different Backlot testing projects.

When adding or changing a component:

1. Keep its default behavior simple and predictable.
2. Allow configuration when necessary.
3. XML documentation alongside the implementation. Document its purpose, supported behavior, and relevant limitations.
4. Update the README only when the package setup, customization workflow, or contributor instructions change.

### Building

**Build the NuGet package:**

```bash
dotnet pack Backlot.Testing.Defaults/Backlot.Testing.Defaults.csproj -c Release
```