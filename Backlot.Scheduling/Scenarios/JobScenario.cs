#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type - backlot framework takes care of this.
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Scheduling.Roles;
using Cronos;

namespace Backlot.Scheduling.Scenarios;

public interface IJobScenario: IScenario
{
    TimeProvider? Time { get; set; }
}

[SuppressMessage("ReSharper", "PossibleStructMemberModificationOfNonVariableStruct")]
public abstract class JobScenario<TRole> : Scenario<TRole, bool>, IJobScenario where TRole : IJob
{
    # region overall job management
    /// <summary>
    /// Static ConcurrentDictionary to track running jobs.
    /// and avoid duplicate job runs.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> Running = new();
    
    /// <summary>
    /// Timer of this job.
    /// </summary>
    public TimeProvider? Time { get; set; }
    
    /// <summary>
    /// Next run of this job.
    /// </summary>
    public DateTimeOffset? NextRun { get; set; }
    #endregion
    
    
    protected JobScenario(TRole role, string named=null) : base(role, named)
    {
        Playing += OnPlaying;
        Played += OnPlayed;
        Error += OnError;
        After += OnAfter;
    }

    public override bool Validate()
    {
        var valid = base.Validate();
        if (valid)
        {
            if (!Reference.Equals(Role.Scenario))
            {
                ValidationResults.Add(new ValidationResult($"{Role.Uid} scenario is not of type {Reference.Name}"));
                return false;// immediatly stop checking
            }
            
            if (!Running.TryAdd(Role.Uid, 0))
            {
                ValidationResults.Add(new ValidationResult($"{Role.Uid} job is already running."));
                return false; // immediatly stop checking
            }
        }

        return valid;
    }

    private Task OnPlaying(object sender, EventArgs e)
    {
        Role.LastStatus = JobStatus.Running;
        return Task.CompletedTask;
    }
    
    private Task OnPlayed(object sender, EventArgs e)
    {
        Role.NextRun = NextOccurrence(Role, Role.LastRun ?? DateTimeOffset.MinValue);
        Role.LastRun = Time?.GetUtcNow() ?? DateTime.UtcNow;
        Role.LastStatus = JobStatus.Succeeded;
        return Task.CompletedTask;
    }
    
    private Task OnError(object sender, ExceptionEventArgs @event)
    {
        if(@event.Exec) // last status can only be set to failed before persistance.
            Role.LastStatus = JobStatus.Failed;
        
        return Task.CompletedTask;
    }
    
    private Task OnAfter(object sender, EventArgs e) // finally.
    {
        Running.TryRemove(Role.Uid, out _);
        return Task.CompletedTask;
    }


    private static DateTimeOffset NextOccurrence(IJob schedule, DateTimeOffset after)
    {
        var format = schedule.Cron.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 6
            ? CronFormat.IncludeSeconds
            : CronFormat.Standard;
        var zone = string.IsNullOrWhiteSpace(schedule.TimeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);

        return CronExpression.Parse(schedule.Cron, format).GetNextOccurrence(after, zone) ?? DateTimeOffset.MinValue;
    }
}