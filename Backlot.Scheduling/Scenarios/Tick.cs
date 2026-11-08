using Backlot.Core;
using Backlot.Core.Abstraction.Configuration;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Services;
using Backlot.Scheduling.Roles;
using Cronos;
// ReSharper disable MemberCanBePrivate.Global : public values which are configurable.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // PlayInOwnScope is internally set by the background service.

namespace Backlot.Scheduling.Scenarios;

/// <summary>
/// Scenario; Plays <see cref="JobScenario"/> for every due <see cref="IJob"/>, each within its own scope and unit of work. Tick is started by <see cref="Scheduler{T}"/>.
/// Tick scenario itself does not have a reference in the scenario repository for security reasons.
/// </summary>

public class Tick(IDirector role, IPersistedRoleRepository roleRepository) : DirectorScenario<Tick, bool>(role)
{ // NOT available as scenario reference and therefore not marked with [Scenario ..]
    
    public TimeProvider? Time { get; internal set; }
    public CancellationToken CancellationToken { get; internal set; }
    
    /// <summary>
    /// Composition to execute the <see cref="JobScenario"/> scenario for the given <see cref="IJob"/>, within its own scope.
    /// </summary>
    public Func<IJob, TimeProvider, CancellationToken, Task> PlayScoped { get; internal set; }
    
    [Configurable]
    public int MaxTotalOfEnabledItems { get; set; } = 99;

    protected override async Task<bool> ExecAsync()
    {
        var enabledSchedulers = roleRepository.GetAll<IJob>(1, MaxTotalOfEnabledItems, out _, new[]
        {
            new Criteria { Field = nameof(IJob.Enabled), Condition = "eq", Value = true }
        }).ToList();
        
        foreach (var schedule in enabledSchedulers)
        {
            try
            {
                await PlayScoped(schedule, Time, CancellationToken);
            }
            catch
            {
                // todo: add exception handling and logging the right way.
                // failed, record it below.
                // keep running
            }
        }

        return true;
    }
}
