# Backlot.Scheduling

Plays scenarios on a cron schedule. A schedule is a persisted `IJob` record, managed like any other persisted role. A background service ticks at a fixed interval and plays every enabled job that is due.

## Important: run the scheduler on one instance only

Register the scheduler on **exactly one instance** of your hosting infrastructure. Do **not** run it on multiple load balanced or scaled out instances.

The scheduler has no distributed locking or leader election at the moment.

- Every instance with the scheduler registered runs its own timer, reads the same `IJob` records and plays the jobs that are due.
- `NextRun` is saved after the job has been played. Until then, other instances still see the job as due.

Recommended setups:

- **One dedicated worker** (console app, Windows service, container with one replica) that registers the scheduler. Your load balanced web instances leave it out.
- **One web app with a config switch**, so only one instance enables the hosted service:

## Registration

```csharp
// Program.cs
builder.Services.AddScheduler<AutofacScopedPlayer>();
```

## The job record (`IJob`)

| Property | |
|---|---|
| `Scenario` | Required. The scenario to play, `{scenario}.{named}` for named configurations. JobScenarios only! |
| `Cron` | Required. Five fields (minute precision) or six fields (with seconds). Macros like `@daily` work too, see [Cronos](https://github.com/HangfireIO/Cronos). |
| `TimeZone` | IANA or Windows time zone id that the cron expression uses. UTC when empty. |
| `Enabled` | The scheduler only plays enabled jobs. Disabled jobs can still be played manually. |
| `NextRun` | The next moment the job is due. The scheduler updates it after each successful run. |
| `LastRun` | When the last successful run finished. |
| `LastStatus` | `Running`, `Succeeded` or `Failed`. |

You can extend `IJob` to pass parameters to the scheduled scenario:

```csharp
public interface IHeart : IJob
{
    int Beats { get; set; }
}
```

## Scheduled scenarios

The job record is the role passed to the scheduled scenario:

```csharp
[Scenario(typeof(Beat), access: [Access.Admin])] // or user the name of the SuperAdmin at Backlot.Scheduling
public class Beat(IHeart role) : JobScenario<IHeart>(role)
{
    protected override bool Exec()
    {
        Role.Beats += 1; // Increment the Beats with one beat
        return true;
    }
}
```

## How a tick works

1. `Scheduler<T>` (a `BackgroundService`) waits for the next tick and plays the `Tick` scenario.
2. `Tick` loads the enabled `IJob` records, up to `MaxTotalOfEnabledItems` (configurable, default 99).
3. For each job, the `IScopedPlayer` starts a new DI scope as the scheduler user and checks whether `NextRun` is in the past.
4. If so, it plays the scenario reference which implements the JobScenario base class, which does all the plumbing for registering job status.
5. The unit of work is committed only if the run succeeded. On failure nothing is committed, so the played scenario's partial changes are rolled back and the job stays due.

## Manual runs

Each implementation of `JobScenario` is a normal scenario. It can be be played directly using its `IJob`. Whether or not the job is enabled or due. A manual run does not update `NextRun`.

## Security

The scheduler plays its jobs as `SchedulersUserContext`, user name `scheduler`. This internal user is in the `Admin` and `Everyone` groups and acts like an admin. It cannot be initialized from a token.

- The job record and it's corresponding scenario must grant `scheduler`, `Everyone` or `Admin`, so it can load the job, play it and save the run state.
- `Tick` is not registered as a scenario reference, so it can't be played over HTTP.

## Scoped player

`IScopedPlayer.RunScoped` decides how a job runs in isolation. An implementation must:

- start a new DI scope with `SchedulersUserContext` registered as `IUserContext`,
- skip the job unless `NextRun` is in the past,
- play `Run` for the job, using the `nextOccurrence` function to set `NextRun` once the run has been played,
- commit the scope's `IUnitOfWork` only if the run succeeded.

`AutofacScopedPlayer` is the default implementation part of this package.

## Example job

And example job;

```json
{
  "Uid": "24ddefb4396148e09cb00fc3e25fd08f",
  "Name": "Heartbeat Example",
  "Scenario": "Beat",
  
  "Cron": "*/5 * * * *",
  "TimeZone": "Europe/Amsterdam",
  
  "Enabled": true,
  "NextRun": "2026-11-03T10:05:00+00:00",
  "LastRun": "2026-11-03T10:00:00.1234567+00:00",
  
  // any Job specific properties you like to add
  "Beats": 42
}
```