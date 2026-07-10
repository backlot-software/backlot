# AGENTS.md

This file provides guidance to Agents when working with code in this repository.

## Commands

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test Backlot.Testing/Backlot.Testing.csproj

# Run a specific test class or method
dotnet test Backlot.Testing/Backlot.Testing.csproj --filter "FullyQualifiedName~<TestClass>"

# Run a demo app
dotnet run --project Backlot.Demo.Web/Backlot.Demo.Web.csproj
dotnet run --project Backlot.Demo.Console/Backlot.Demo.Console.csproj
```

### Build configurations
- `Debug` — appends `alpha-<date>` version suffix; used for feature branches
- `Development` — appends `beta` suffix; targets the `develop` branch
- `Release` — clean version; targets `main`

Publishing to GitHub Packages is triggered manually via `workflow_dispatch` in `.github/workflows/`.

---

## Architecture: Movie Pattern Practice (MPP)

Backlot is a framework built on top of .NET 10 that structures all application logic around four concepts:

### Roles (`IRole`)
Interfaces defining **what** something can do. They are the abstraction layer — concise contracts with no implementation. Any object can "present" as a role via `Acting.Presents<TRole>()`, which creates a dynamic Castle proxy. The role name used in HTTP routing strips the leading `I` prefix (e.g., `IProduct` → `Product`).

Key role markers:
- `IPersist` — role is automatically saved to the database after a scenario runs; requires `IPermission` and `IUid`
- `IUid` — role has a unique ID; relations between `IUid` roles are stored automatically
- `IPermission` — role carries read/write/execute permission flags, checked before scenario execution
- `IDirector` — the special singleton "director" role used as the main role for scenarios that have no natural actor

### Scenarios (`Scenario<TRole, TResult>`)
Classes implementing **what the code actually does**. Each scenario has exactly one main role (`TRole`) and returns a result (`TResult`). Inherit from `Scenario<TRole, TResult>` and override `Exec()` (sync) or `ExecAsync()` (async).

Scenarios are discovered at runtime by name via `Loader` (reflection across all loaded assemblies). They must be decorated with `[Scenario]` or a custom `IScenarioInfo` attribute. The attribute links the scenario to its allowed role type so the HTTP routing can find it.

Lifecycle events (in order): `Before` → permission check → `Validate()` → `Playing` → `ExecAsync()` → persist/relate → `Ending` → `After` → `Fired`.

Constructor parameters:
- Role parameters come first and are resolved automatically (from the request, or from stored relations)
- Non-role parameters are resolved from the DI container
- Use `[NullAllowed]` on a role parameter to allow an empty-shell actor when no value is available
- Use `[ExcludeValidation]` on a role property or the role interface itself to skip DataAnnotation validation

### Watchers (`ISceneWatcher<TScenario>`, `IBingeWatcher`)
React to completed scenarios. `ISceneWatcher<T>` watches a specific scenario; `IBingeWatcher` watches all scenarios. Registered in the Director's `Incept()` via `Watch<TScene, TWith>()` or `WatchAll<TWith>()`.

### Director (`Director` / `IDirector`)
The application orchestrator. Subclass `AutofacContainerDirector` (or equivalent) and implement:
1. `Registration()` — register DI services (call `base.Registration()` first)
2. `Incept()` — wire up instructors, expression engines, compositions, and watchers

**Instructors** (`AssignInstructorFor<TRole>(func, priority)`) — transform a role after it is presented. Used primarily for permission initialization (e.g., `PermissionInitialization.DbAccessInitialization`).

**Compositions** (`PrepareCompositionFor<TScenario>(action)`) — inject additional behaviour into a scenario instance before it runs.

---

## HTTP API Convention

The web host (`Backlot.WebApp`) exposes a convention-based API:

```
GET  /api/role/{rolename}/{scenario}?uid=<uid>
POST /api/role/{rolename}/{scenario}          (body: JSON role data)
```

- `{rolename}` is case-insensitive and resolved by `Loader.GetRoleByName()` — matches the interface name without the `I` prefix
- `{scenario}` matches the `Name` property of the scenario's `IScenarioInfo` attribute
- For `IDirector`, no `uid` is needed; for other roles, `?uid=` must be the only query param on GET
- Named scenario variants use dotted notation: `{scenario}.{configname}`
- Responses honour the `Accept` header via `IMediaFormatResolver` (JSON default, CSV and HTML via optional formatter packages)

---

## Dependency Injection

Backlot uses **Autofac**. `ServiceLocator` is the static access point used throughout the framework internals (scenarios, actors). It is configured per-request in the web host via `ServiceLocator.Configure(context.RequestServices)`.

Never call `ServiceLocator.Configure()` in application code — it is set up by the framework startup and middleware.

---

## Persistence

`IPersistedRoleRepository` abstracts storage. Implementations:
- `MemoryPersistedRoleRepository` — in-memory, used in tests
- `LitePersistedRoleRepository` (`Backlot.Services.LiteDB`)
- RavenDB (`Backlot.Services.RavenDB`)
- SQL (`Backlot.Services.SqlDb`)

Relations between `IUid` roles are stored separately in `IRelationRepository`. The `ScenarioBuilder` uses stored relations to auto-resolve missing constructor role parameters.

---

## Testing

Tests live in `Backlot.Testing` and use **NUnit** + **NSubstitute**.

Bootstrap a test with `Initialize.Setup()`:

```csharp
[SetUp]
public void Setup() => Initialize.Setup();
```

This wires up Autofac with in-memory repositories and calls `director.Incept()`. Each call to `Setup()` flushes the in-memory database, so tests are isolated. Pass a custom `buildDirector` lambda to override the default test director.

---

## Key Source Locations

| Concern | Path |
|---|---|
| Core interfaces & base classes | `Backlot.Core/` |
| Role proxy / acting system | `Backlot.Core/Abstraction/Actors/` |
| Scenario base & builder | `Backlot.Core/Abstraction/Scenarios/` |
| Security / user context | `Backlot.Core/Security/` |
| Reflection / role name resolution | `Backlot.Core/Loader.cs` |
| Opinionated defaults | `Backlot.Defaults/` |
| ASP.NET host & routing | `Backlot.WebApp/ApplicationBuilding.cs` |
| HTTP middleware | `Backlot.Http/Middleware/` |
| Autofac DI director base | `Backlot.DependencyInjection.Autofac/` |
| Test bootstrap | `Backlot.Testing/Initialize.cs` |
| Web demo (reference implementation) | `Backlot.Demo.Web/` |

## Techstack

The techstack we use is;
- Hotwired TurboJS and Stimulus: for that single page application feeling and all javascript needs.
- Razor Pages: and cshtml.cs code behind to do all interactive work
- Tailwind: to utilize styles 

Other javascript frameworks than the Hotwired stack are not allowed. Javascript written besides these frameworks need to be vanilla.