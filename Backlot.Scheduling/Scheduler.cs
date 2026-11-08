using Backlot.Core.DependencyInjection;
using Backlot.Scheduling.Scenarios;
using Microsoft.Extensions.Hosting;

namespace Backlot.Scheduling;

/// <summary>
/// <see cref="BackgroundService"/> which waits for next tick to start a <see cref="Tick"/> scenario.
/// </summary>
internal sealed class Scheduler<T>(
    IServiceProvider serviceProvider,
    TimeSpan interval,
    TimeProvider? timeProvider = null) : BackgroundService
    where T : IScopedPlayer, new()
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval, _time);
        
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ServiceLocator.Configure(serviceProvider);
                await Tick.Play(t => // pass time parameter to Tick.
                {
                    t.Before += (_, _) =>
                    {
                        t.PlayScoped = new T().PlayScoped;
                        t.CancellationToken = stoppingToken;
                        t.Time = _time;
                        return Task.CompletedTask;
                    };
                });
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // stopping
        }
    }
}
