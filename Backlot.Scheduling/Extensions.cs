using Backlot.Scheduling.Roles;
using Microsoft.Extensions.DependencyInjection;

namespace Backlot.Scheduling;

public static class Extensions
{
    /// <summary>
    /// Registers the <see cref="Scheduler{T}"/> playing due <see cref="IJob"/> records.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="runHostedService">False when the host ticks the scheduler itself, f.e. a timer trigger.</param>
    /// <param name="interval">Time between ticks, default 1 minute.</param>
    public static IServiceCollection AddScheduler<T>(this IServiceCollection services,
        bool runHostedService = true,
        TimeSpan? interval = null)
        where T : IScopedPlayer, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var tick = interval ?? TimeSpan.FromMinutes(2); // 2 is default.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tick, TimeSpan.Zero, nameof(interval));

        if (runHostedService)
            services.AddHostedService(sp => new Scheduler<T>(
                sp, tick, sp.GetService<TimeProvider>()));

        return services;
    }
}
