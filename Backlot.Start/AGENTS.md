# AGENTS.md
You are a software developer following [Backlot]([Backlot](https://www.backlot.software)) development practices and guidelines. You build application code with the Backlot framework (.NET 10), in a project created from the Backlot.Start template. Apply the concepts, rules and templates in this file to everything you design, write and review. The API described here follows the develop branch of backlot-software/backlot; where older documentation shows a different API, this file wins.

## 1. Mental model: the Movie Pattern Practice (MPP)

Every piece of application logic is expressed with four concepts. Do not introduce controllers, services-with-business-logic, repositories-per-entity or mappers next to them.

| Concept | What it is | In code |
|---|---|---|
| **Role** | *What something can do.* A contract represented by an actor describing the data a scenario needs. | An interface extending `IRole` |
| **Actor** | Any object (JSON, dictionary, anonymous or typed object) that plays a role. | Not a type you write; it is *presented* as a role |
| **Scenario** | *What the code actually does.* One unit of business logic with one main role and one result. Implemented within `Exec()` or `ExecAsync()`  | A class extending `Scenario<TScenario, TRole, TResult>` or one of its abstract defaults (such as `DirectorScenario` or `JobScenario`) |
| **Watcher** | *Reacts to what happened.* Subscribes to scenario events for follow-up work. | `ISceneWatcher<TScenario>` or `IBingeWatcher` |
| **Director** | *Orchestrates.* Wires storage, auth, instructors, compositions and watchers. | By default we extend `AutofacContainerDirector<…>` |

Principles to keep in mind while designing:

- **Proxying, not mapping.** An actor is never converted or discarded. Backlot wraps it in a dynamic proxy that exposes the role interface. Properties the role does not define stay on the actor and survive the scenario.
- **Skills.** An actor can present as several roles. Every role interface it satisfies (including inherited ones) is a *skill*. Query by skill, not by concrete type.
- **Relations instead of joins.** When two uniquely identified roles play in the same scenario, they become related. Relations are unordered pairs, stored once.
- **Persistence is a side effect of playing.** You do not call "save". Declaring a role as `IPersist` is what makes it stored. When IEnumerable of IPersists need to be persisted within the scenario do this manually. Ask the programmer how to handle these. Do never call `PersistAndRelate` for this because this does create relations for all objects as well, which is in most cases not what you want.
- **Convention over wiring.** A scenario class plus its attribute is a complete feature: HTTP endpoint, in-process API, authorization, validation and storage follow from it.

## 2. Project layout and commands

```
<App>/
  Program.cs                 host startup only (filesystem, configuration, director, middleware)
  WebDirector.cs             the single Director of the application
  Roles/                     role interfaces, one concept per file
  Scenarios/                 scenario classes, grouped in sub-namespaces per capability
  Watchers/                  watcher classes (create when first needed)
  Instructing/               instructor functions (create when first needed)
  local.jsonsettings.json    Backlot configuration for environment "local"
  usersandgroups.json        users/groups for Basic auth (never commit real hashes) not needed when a custom implementation of `IUserRepository` is defined 
  appsettings.json           ASP.NET + BacklotStudio section
```

```bash
dotnet new install Backlot.Start        # once per machine
dotnet new backlot-start -n <App>       # scaffold
dotnet build
dotnet run                              # API under /api, Backlot Studio under /studio
dotnet test
```
## 3. Roles

A role is an interface. It carries no behaviour.

**Marker interfaces decide what the framework does with a role:**

| Extends | Effect |
|---|---|
| `IRole` | Plain role. Lives for the duration of the scenario only. |
| `IUid` | Has a required `Uid`. Can be referenced and related to other `IUid` roles. |
| `IPermission` | Carries read/write/execute permissions that are checked when it plays. |
| `IPersist` | `IUid` + `IPermission` + `Name` + `LastModified`. Stored automatically after a scenario runs. |
| `IDirector` | The singleton director; main role of scenarios that have no natural actor (lists, lookups, status). |

**Rules**

- Name role interfaces with a leading `I`. The public role name is the interface name without it (`IOrder` → `order`), is case-insensitive, and must be unique across all loaded assemblies.
- Model a role as the *minimum* a scenario needs. Compose larger roles through interface inheritance (`ICart : IShoppingList`), which also adds skills.
- Mark properties that scenarios or the database fill with `[Calculated]` (`Backlot.Core.Json`). Calculated values are not accepted as input.
- Validate with DataAnnotations on the interface (`[Required]`, `[Range]`, `[RegularExpression]`). Use `[ExcludeValidation]` on a role interface or scenario property to opt out.
- Never set `LastModified` or `__Permission` directly. The repository owns the first; use `ManagePermission` for the second.
- `Uid` and `Name` need a public setter (or constructor) on typed actors, otherwise deserialization fails.
- Prefer "become" actors (anonymous objects, dictionaries, JSON, POCOs). Only write a class that implements the role itself ("self") when you must, and still present it.

**Template: roles**

```csharp
using System.ComponentModel.DataAnnotations;
using Backlot.Core;
using Backlot.Core.Json;

namespace MyApp.Roles;

// Transient input role
public interface IMessage : IRole
{
    [Required]
    string Message { get; set; }
}

// Persisted role
public interface IOrder : IPersist
{
    [Required]
    string CustomerEmail { get; set; }

    decimal Amount { get; set; }

    [Calculated]                // filled by scenarios, never by the request
    string Status { get; set; }
}
```

### Presenting actors

Presenting turns an actor into a role and runs the instructors the director assigned for each of its skills.

```csharp
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;

var order = new { Uid = Uid.New(), Name = "Order 1", CustomerEmail = "a@b.c", Amount = 10m }
    .Presents<IOrder>();                       // known role type

IRole role = actor.PresentsType(roleType);     // role type only known at runtime
var empty  = Acting.New<IOrder>();             // new role backed by an empty-shell actor
```

- `role.Skills()`, `role.IsOfSkill<T>()` and `roles.OfSkill<T>()` inspect and filter by skill.
- `role.IsNull()` tells whether an optional role is an empty shell.

## 4. Scenarios

One scenario is one use case. It has exactly one **main role** and one **result**. Results are allowed to return any type. If the type inherits IPersist it's persisted automatically, if not then not.

**Rules**

- Inherit from `Scenario<TScenario, TRole, TResult>`; use `DirectorScenario<TScenario, TResult>` when the director is the main role. The self-referencing `TScenario` argument is what enables the static `Play`/`With` API.
- Decorate with `[Scenario(typeof(TScenario), access: [...])]`. Without the attribute the scenario is not reachable over HTTP. With the attribute but no `access`, nobody can call it from outside.
- The scenario name is the class name. It is matched case-insensitively together with the main role type, so the same name may exist for different roles. But we advice to use one scenario name only once.
- Override **either** `Exec()` (sync) **or** `ExecAsync()` (async). Never both, and never call the base implementation.
- **Constructor parameter order is significant:** main role first, then supporting roles, then services resolved from DI, optionally `string named = null` last. The builder uses the constructor with the most parameters.
- Supporting roles are resolved in this order: supplied by the caller → loaded from the stored relations of the supplied persisted roles (exactly one match required) → empty shell if the parameter has `[NullAllowed]` → otherwise the scenario fails to build.
- Keep scenarios small and stateless. To reuse logic, play another scenario; do not inherit from concrete scenarios or share static state.
- Do not read `[Configurable]` properties or composition hooks in the constructor: watchers and compositions are attached during base construction, configuration is resolved just after it.


**Lifecycle of `Start()`**

1. `Before` (awaited; HTTP authorization happens here)
2. Permission check: main role must be executable, all roles readable
3. `Validate()` — DataAnnotations on all public role properties; failing raises `ValidationException`
4. `Playing`
5. `ExecAsync()` / `Exec()`
6. `Played`
7. Persist and relate (unless `PersistAndRelate` is overridden to `false`)
8. `Ending`
9. `After` (always runs, also after a failure); `Error` is raised when something throws

`Fired` is raised after every event fired through `FireAsync` (`Playing`, `Played`, `Ending` and custom events).

**What gets persisted and related**

- Only **public properties** of the scenario whose declared type is a role take part: `Role`, `ResultValue`, and any property you expose yourself. A supporting role kept in a private field is neither validated, persisted nor related.
- The **declared type** decides, not the runtime value: `IPersist`-typed properties are saved, `IUid`-typed properties are related to each other.
- After saving, the properties are replaced by the merged version from the repository, so give exposed role properties at least a private setter.
- Read-only scenarios must declare `protected override bool PersistAndRelate => false;`.
- Returning an `IPersist` as result stores it. Return a non-persist role, a DTO when the result or must not be saved.

**Template: scenario with a role**

```csharp
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using MyApp.Roles;

namespace MyApp.Scenarios.Orders;

[Scenario(typeof(PlaceOrder), access: [Access.Everyone])]
public class PlaceOrder(IOrder role)
    : Scenario<PlaceOrder, IOrder, IOrder>(role)
{
    protected override IOrder Exec()
    {
        Role.Status = "Placed";
        return Role;            // IOrder is IPersist: saved automatically after Exec
    }
}
```

**Template: director scenario (no input role, read-only)**

```csharp
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Backlot.Core.Services;
using MyApp.Roles;

namespace MyApp.Scenarios.Orders;

[Scenario(typeof(OpenOrders), access: [Access.Admin])]
public class OpenOrders(IDirector role, IPersistedRoleRepository roles)
    : DirectorScenario<OpenOrders, IEnumerable<IOrder>>(role)
{
    protected override bool PersistAndRelate => false;

    protected override IEnumerable<IOrder> Exec()
    {
        return roles.GetAll<IOrder>(page: 1, pageSize: 50, out _,
            criteria: [new Criteria { Field = nameof(IOrder.Status), Condition = "eq", Value = "Placed" }]);
    }
}
```

**Template: supporting roles, services, custom validation, async**

```csharp
[Scenario(typeof(ShipOrder), access: [Access.Admin])]
public class ShipOrder : Scenario<ShipOrder, IOrder, bool>
{
    private readonly IShippingClient _shipping;

    // public + role-typed => validated, and persisted/related according to its declared type
    public ICustomer Customer { get; private set; }

    public ShipOrder(IOrder role, [NullAllowed] ICustomer customer, IShippingClient shipping) : base(role)
    {
        Customer = customer;
        _shipping = shipping;
    }

    public override bool Validate()
    {
        if (Role.Status != "Placed")
            ValidationResults.Add(new ValidationResult("Only placed orders can be shipped."));

        return base.Validate();   // keeps role validation; false when ValidationResults is not empty
    }

    protected override async Task<bool> ExecAsync()
    {
        await _shipping.Ship(Role.Uid);
        Role.Status = "Shipped";
        return true;
    }
}
```

### Playing scenarios from code

```csharp
var result = await PlaceOrder.Play(order);                        // role or raw actor
var shipped = await ShipOrder.With(order).With<ICustomer>(customer).Play();  // several roles
var open = await OpenOrders.Play();                               // director scenario
var res = await new ScenarioReference { Name = "PlaceOrder" }.Play(order);   // by name
```

- In-process `Play` enforces **role permissions and validation, but not the `access` list**. The `access` list is enforced only on the HTTP path. Do not rely on `access` to protect a scenario that other code plays.
- An optional `watch` argument (`Play(role, s => s.Ending += …)`) subscribes to events for that single run.

### Named configurations and compositions

- **Configuration:** mark public `string`, `int`, `decimal`, `float` or `bool` properties with `[Configurable]` (`Backlot.Core.Abstraction.Configuration`). Values are read from configuration at `<Namespace>.<ClassName>.<Property>`.
- **Named variants:** a scenario whose constructor has a `string named = null` parameter (passed to `base(role, named)`) can be played as `{scenario}.{name}`; its settings are then read from `<Namespace>.<ClassName>.<name>.<Property>`.
- **Composition** replaces the provider pattern. The scenario exposes a settable delegate property with a sensible default; the director fills it in per application. Use it when the same scenario must behave differently per installation or customer.

```csharp
// scenario
public Func<IOrder, Task<decimal>> CalculateTax { get; set; } = _ => Task.FromResult(0m);

// director, inside Incept()
PrepareCompositionFor<PlaceOrder>(s => s.CalculateTax = order => Task.FromResult(order.Amount * 0.21m));
```

## 5. Events and watchers

Watchers decouple follow-up work (notifications, webhooks, audit, starting another scenario) from the scenario that causes it. Business rules that decide the scenario's own result belong in the scenario, not in a watcher.

- `ISceneWatcher<TScenario>` watches one scenario type; `IBingeWatcher` watches every scenario.
- A watcher's `Watch(scenario)` runs when the scenario is constructed and only subscribes to events.
- Watchers are resolved from DI, so constructor injection works, and their `[Configurable]` properties are resolved from configuration (configuration wins over code).
- Exceptions thrown by handlers of `Playing`, `Played`, `Ending` and custom events are logged and swallowed. Exceptions in `Before` handlers abort the scenario.
- `After` also fires when the scenario failed. Check `scenario.ResultValue` or subscribe to `Ending` when the reaction must only follow success. This applies to `FollowUpScenarioWatcher<TWatch, TStart>` too, which starts `TStart` with the watched scenario's roles on `After`.

**Template: custom event on a scenario**

```csharp
public event AsyncEventHandler<EventArgs>? Shipped;

protected override async Task<bool> ExecAsync()
{
    // ...
    await FireAsync(Shipped, nameof(Shipped), EventArgs.Empty);
    return true;
}
```

**Template: scene watcher**

```csharp
using Backlot.Core;
using Microsoft.Extensions.Logging;
using MyApp.Scenarios.Orders;

namespace MyApp.Watchers;

public class OrderPlacedWatcher(ILogger<OrderPlacedWatcher> logger) : ISceneWatcher<PlaceOrder>
{
    public void Watch(PlaceOrder scenario)
    {
        scenario.Ending += (_, _) =>
        {
            logger.LogInformation("Order placed, within '{Clss}.{Fn}'", nameof(OrderPlacedWatcher), nameof(Watch));
            return Task.CompletedTask;
        };
    }

    void IWatcher.Watch(IScenario scenario) => Watch((PlaceOrder)scenario);
}
```

## 6. Director

There is exactly one director per application. It is the only place for wiring.

- **Type arguments** select infrastructure: relation repository, persisted-role repository, unit of work, user context, cache factory. Swapping LiteDB for RavenDB or SQL, or Basic for another auth provider, is a change here and nowhere else.
- **`Registration()`**: DI registrations. Call `base.Registration()` first.
- **`Incept()`**: expression engines, instructors, compositions, watchers — in that order.
- **`SecretKey`**: used for token and permission encryption. Must be unique per installation and come from a secret store in production.

**Instructors** are functions `(role, actor) => role` that prepare a role right after it is presented, for every matching skill. Typical uses: defaults, aliases, permissions, derived fields. Lower priority numbers run first; unprioritised instructors run in registration order before prioritised ones. Keep the three permission instructors last, in the order shown.

**Template: director**

```csharp
public class WebDirector(IFileSystem fileSystem, IConfigurationManager configurationManager, ContainerBuilder builder)
    : AutofacContainerDirector<
        LiteRelationRepository,
        LitePersistedRoleRepository,
        DummyUnitOfWork,
        BasicUserContext,
        CacheFactory>(fileSystem, configurationManager, builder)
{
    protected override string SecretKey => "<from secret store>";

    public override void Registration()
    {
        base.Registration();

        Builder.RegisterType<UserFileRepository>().As<IUserRepository>().SingleInstance(); // used by BasicUserContext
        Builder.RegisterType<ShippingClient>().As<IShippingClient>().InstancePerLifetimeScope();
    }

    public override void Incept()
    {
        // 1) expression engines
        // AssignExpressionEngineFor<string, MustachExpressionEngine>();

        // 2) instructors
        AssignInstructorFor<IRole>(Instructors.CaseInsensitiveInitializer);
        AssignInstructorFor<IOrder>(OrderInstructing.Defaults);

        AssignInstructorFor<IPersist>(PermissionInitialization.EncryptedPermissionInitialization, 997);
        AssignInstructorFor<IPersist>(PermissionInitialization.DbAccessInitialization, 998);
        AssignInstructorFor<IPermission>(PermissionInitialization.AllAccessInitialization, 999);

        // 3) compositions
        PrepareCompositionFor<PlaceOrder>(s => s.CalculateTax = ...);

        // 4) watchers
        Watch<PlaceOrder, OrderPlacedWatcher>();
        // optional and use with caution; WatchAll<DebugWatcher>();
    }
}
```

**Template: instructor**

```csharp
namespace MyApp.Instructing;

public static class OrderInstructing
{
    public static IOrder Defaults(IOrder order, object actor)
    {
        order.Status ??= "New";
        return order;            // always return the role
    }
}
```

Aliases: Aliasses are part of `Backlot.Defaults.Instructing` and are a default instructor. Put `[Alias(["Op", "operatie"])]` on a role property (or `[FieldInfoAlias("Uid", ["Id"])]` on the interface) and assign `Instructors.AliasInitializer` for that role or its parents, so actors with differently named fields can play it. Alias names are case sensitive.

`Program.cs` only builds the host: `BuildWebApp` + `ConfigureBacklotWeb` returning the director, then the middleware in this fixed order — `AutofacScopeExecutor`, `Defender`, `AuthenticationInitializer`, optionally `SerilogContextEnrichment`. Do not add business logic, endpoints or controllers there.

## 7. HTTP convention

Every scenario with a `[Scenario]` attribute is exposed; never hand-write endpoints for scenarios.

```
GET  /api/role/{role}/{scenario}?uid={uid}     persisted role, loaded from storage (uid is the only parameter)
GET  /api/role/{role}/{scenario}?a=1&b=2       non-persisted role, built from the query string
GET  /api/role/director/{scenario}             director scenario
POST /api/role/{role}/{scenario}               body = the role as JSON; no query string
POST /api/role/director/{scenario}             body = { "<roleName>": { ...role... } | "<uid>", ... }
GET  /api/status                               liveness and version
```

- `{scenario}.{name}` selects a named configuration.
- **POST** supplies the role data and, for persisted roles, merges with and updates the stored record. **GET** works on what is stored; the scenario it runs may still change data.
- JSON responses are wrapped: `{ "ExecutionTime", "TimeInMs", "Body", "Status": "OK" | "FAIL" }`. Other formats follow the `Accept` header through registered `IMediaFormatter`s.
- Status mapping: `NotFoundException` → 404; `BadRequestException` and validation failures → 400; `UnauthorizedAccessException` and `PermissionControlException` → 401; anything else → 500. Throw these types from scenarios instead of crafting responses.
- The unit of work is committed only when the response is 200.
- `access` values: `Access.Open` (anyone, including anonymous — never return secrets), `Access.Everyone` (any authenticated user), `Access.Admin`, or explicit group and user names.
- Request bodies must not contain `__Skills`; the `Defender` middleware rejects them.

## 8. Security

Two independent layers; design for both.

1. **Scenario access** (`access` on the attribute): who may *call* the scenario over HTTP.
2. **Role permissions** (`IPermission`): what the current user may do with a *specific record*. Unix-inspired levels `None`, `Read`, `ReadExecute`, `ReadWrite`, `ReadWriteExecute`, set as a mask plus per-group and per-user levels. The mask caps everything; a user-specific level beats group levels; otherwise the highest matching group level applies.

- The main role needs execute permission, supporting roles need read permission, writing is enforced by the repository, and a result role the user cannot read is blanked.
- Set record permissions inside the scenario that creates the record:

```csharp
order.ManagePermission(p => p
    .SetGroup(Access.Admin, PermissionLevel.ReadWriteExecute)
    .SetUser(UserContext.Current.UserName, PermissionLevel.ReadWrite));
```

- Read the caller through `UserContext.Current` (`UserName`, `Groups`, `IsInGroup`, `IsAuthenticated`). One `IUserContext` per installation; its scheme is `Basic` or `Bearer`.

## 9. Persistence, relations and search

- Backlot is not an ORM and not a search engine. Storage is two collections: persisted roles and relations. Do not design tables, foreign keys or join entities.
- Inject `IPersistedRoleRepository` and `IRelationRepository` into scenarios for reads. Let the scenario lifecycle do the writes.
- `GetAll` filters by role type/skill, a flat list of `Criteria` (`Field`, `Condition`, `Value`), paging, a date range and ordering. Use `nameof(IRole.Property)` for `Field`.
- Criteria semantics (faceted search): criteria on the same field form a group; inside a group `eq`/`ct` are OR-ed and `lt`/`gt` are AND-ed (a range); groups are AND-ed.
- Related records: `relationRepository.GetAll(role.GetReference())`, then `roleRepository.GetBulk(references)`.
- Free-text search or OR across fields: add a text property to a role skill, fill it with an instructor, and query it with `ct`.
- Prefer the shipped scenarios in `Backlot.Defaults` before writing your own: `Find` (query), `Detail` (record + relations), `Persist`, `Relate`, `Relations`, `Revisions`, `Transformation`, `IsValid`, `WhoAmI`, `IsAuthenticated`, `AuthInfo`, plus configuration and schema scenarios (`ConfigurationInfos`, `TryUpdateConfiguration`, `TryDeleteConfiguration`, `Roles`, `Scenarios`, `ScenarioSchemas`, `ScenarioSpec`).

## 10. Configuration

- `JsonSettingsManager("<env>", fileSystem)` reads `<env>.jsonsettings.json`; `DuplexConfigurationSettingsManager` layers it over host `IConfiguration`.
- Keys are nested objects following namespace, class and property:

```json
{
  "Container": { "defaultAssembly": "MyApp" },
  "Backlot": { "Services": { "LiteDB": { "Settings": { "ConnectionString": "mydata.db" } } } },
  "MyApp": { "Scenarios": { "Orders": { "PlaceOrder": { "TaxRate": "0.21" } } } }
}
```

- `Container.defaultAssembly` must equal the application assembly name.
- Files and templates are read through the director's `IFileSystem` (`LocalDiskStorage`, `BlobStorage`); do not use `System.IO` directly for application content.

## 11. Scheduling (`Backlot.Scheduling`)

- A schedule is a persisted `IJob` record (`Scenario`, `Cron`, `TimeZone`, `Enabled`, `NextRun`, `LastRun`, `LastStatus`). Extend `IJob` to add job parameters.
- A scheduled scenario inherits `JobScenario<TJob>` and returns `bool`. It remains a normal scenario and can be played manually with its job record.
- Register with `builder.Services.AddScheduler<AutofacScopedPlayer>()` on **exactly one** instance; there is no distributed locking.
- Jobs run as user `scheduler` (in `Admin` and `Everyone`), each in its own DI scope and unit of work; a failed run commits nothing and stays due. Job record and scenario access must allow that user.

## 12. Logging and exceptions

- Use the injected `ILogger<T>`, or the protected `Logger` inside scenarios. Structured templates only, ending with `within '{Clss}.{Fn}'`; include `{ExcType}` and `{ExcMessage}` when logging an exception.
- `LogError`: an exception that is handled and not re-thrown. `LogWarning`: unauthenticated-user situations. `LogInformation`: an exception you throw or re-throw. `LogDebug`: temporary diagnostic detail, the only level that may hold sensitive data, to be removed promptly.
- Never log personal or sensitive data at other levels. Refer to roles with `role.GetFriendlyReference()`.
- Do not catch exceptions in a scenario merely to return a failure value; throw the appropriate exception type and let the host map it.

## 13. Testing

- NUnit, with NSubstitute for interaction-verifying mocks only; prefer the in-memory fakes (`MemoryPersistedRoleRepository`, `MemoryRelationRepository`) and simple stubs.
- Test scenarios by playing them in-process against a test director; do not test through HTTP unless HTTP behaviour is the subject.
- Method-focused tests: one file per method, named `Method_StateUnderTest_ExpectedBehavior`. Use-case tests: one fixture per goal with a user-story summary, tests named `Given…_When…_Then…`. Tests needing real infrastructure go under `Integration/`.
- Arrange / Act / Assert comments, one behaviour per test, `Assert.That` constraint syntax, `Assert.Multiple` only for one behaviour.
- Async tests are `async Task` and awaited — never `.Result`, `.Wait()` or `GetAwaiter().GetResult()`. Expected exceptions via `Assert.ThrowsAsync<T>`.
- Derive cases from the contract: equivalence partitions (for actors: self, dictionary, anonymous, typed, JSON), boundaries, and decision tables for permission/persistence combinations.

**Template: test bootstrap and test**

```csharp
public static class TestApp
{
    public static void Setup()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TestUserContext>().As<IUserContext>();          // your IUserContext stub
        builder.RegisterType<MemoryPersistedRoleRepository>().As<IPersistedRoleRepository>();
        builder.RegisterType<MemoryRelationRepository>().As<IRelationRepository>();
        builder.Register(_ => new EncryptionService("test-secret-0123456789")).As<IEncryptionService>().SingleInstance();
        builder.Register(_ => new ChecksumBuilder(MD5.Create().ComputeHash)).As<IChecksumBuilder>();
        builder.RegisterInstance(new LoggerFactory()).As<ILoggerFactory>();
        builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

        var director = new TestDirector(new FakeStorage(), new FakeConfiguration(new Dictionary<string, string>()), builder);
        director.Incept();                                                   // instructors, compositions, watchers

        ServiceLocator.Configure(new AutofacServiceProvider(builder.Build()));
        ServiceLocator.Get<IPersistedRoleRepository>().FlushDb();            // every test starts clean
    }
}

public class PlaceOrderTests
{
    [SetUp]
    public void Setup() => TestApp.Setup();

    [Test]
    public async Task GivenANewOrder_WhenPlaced_ThenStatusIsPlaced()
    {
        // Arrange
        var order = new { Uid = Uid.New(), Name = "Order 1", CustomerEmail = "a@b.c", Amount = 10m }.Presents<IOrder>();

        // Act
        var result = await PlaceOrder.Play(order);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Placed"));
    }
}
```

## 14. Do and don't

**Do**

- Start every feature by naming the role(s) and the scenario; write the role interface first.
- Reuse roles and default scenarios before adding new ones.
- Keep `access` as narrow as the use case allows and set record permissions where records are created.
- Put per-installation differences in the director (compositions, instructors, configuration), not in `if` branches inside scenarios.

**Don't**

- Don't add MVC controllers, minimal-API endpoints or custom routing for business logic.
- Don't call `Persist`/`TryPersist` or `IRelationRepository.Add` to save what the scenario lifecycle already saves.
- Don't call `ServiceLocator.Configure(...)` in application code; the host and middleware own it. Prefer constructor injection over `ServiceLocator.Get<T>()` (acceptable inside director lambdas).
- Don't map actors to DTOs or entity classes to "clean" them; present them as a narrower role or use `Transmit`.
- Don't block on async code, and don't override both `Exec` and `ExecAsync`.
- Don't put secrets in `*.jsonsettings.json`, `usersandgroups.json` or the director source that is committed.
- Don't register the scheduler on more than one instance.
- Don't use partial classes for scenario implementation, this can cause load problems.