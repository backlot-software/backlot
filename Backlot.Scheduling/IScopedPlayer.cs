using Backlot.Scheduling.Roles;
using Backlot.Scheduling.Scenarios;

namespace Backlot.Scheduling;

/// <summary>
/// Runs a job within its own scope.
/// </summary>
public interface IScopedPlayer
{
    /// <summary>
    /// Run the <see cref="IJob"/> <see cref="IJobScenario"/> within its own scope.
    /// </summary>
    /// <param name="job">The job to play.</param>
    /// <param name="time">The time provider that provides the current time.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the job.</param>
    /// <returns></returns>
    Task PlayScoped(IJob job, TimeProvider time, CancellationToken cancellationToken);
}