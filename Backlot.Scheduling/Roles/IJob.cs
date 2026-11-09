using System.ComponentModel.DataAnnotations;
using Backlot.Core;
using Backlot.Scheduling.Security;

namespace Backlot.Scheduling.Roles;

/// <summary>
/// Persisted role playing <see cref="Scenario"/> on a cron schedule as <see cref="SchedulersUserContext"/>.
/// The job is to execute the scenario on a cron schedule.
/// </summary>
public interface IJob : IPersist
{
    /// <summary>
    /// Scenario to play, "{scenario}.{named}" for named configurations. Its main role must be <see cref="IJob"/>.
    /// </summary>
    [Required]
    // todo: add custom validation to check if scenario exists and has IDirector as main role.
    ScenarioReference Scenario { get; set; }

    /// <summary>
    /// Cron expression; 5 fields (minute precision) or 6 fields (including seconds). Macros like @daily are supported.
    /// </summary>
    [Required]
    string Cron { get; set; }

    /// <summary>
    /// Time zone the cron expression is evaluated in (IANA or Windows id). UTC when empty.
    /// </summary>
    string? TimeZone { get; set; }

    /// <summary>
    /// Disabled schedules are not played by the scheduler, they still can be played manually.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// The next moment the scheduler plays this schedule. Maintained by the scheduler.
    /// </summary>
    DateTimeOffset? NextRun { get; set; }

    /// <summary>
    /// Start of the last run.
    /// </summary>
    DateTimeOffset? LastRun { get; set; }

    JobStatus? LastStatus { get; set; }

    // todo: use logging instead; string? LastError { get; set; }
}