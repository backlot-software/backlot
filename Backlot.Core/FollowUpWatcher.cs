using Backlot.Core.Abstraction.Scenarios;

namespace Backlot.Core;

/// <summary>
/// A watcher that starts another scenario immediatly when the watched scenario ends.
/// </summary>
/// <typeparam name="TWatch"></typeparam>
/// <typeparam name="TStart"></typeparam>
public class
    FollowUpScenarioWatcher<TWatch,
        TStart> // can be a watcher that starts a given scenario when the defined scenario ends.
    : ISceneWatcher<TWatch>
    where TWatch : IScenario
    where TStart : IScenario
{
    public void Watch(TWatch scenario)
    {
        scenario.After += async (sender, _) =>
        {
            var scene = (TWatch)sender;

            if (scene == null) return;
            
            var s = ScenarioBuilder.Construct(typeof(TStart), scene.Roles, null);
            await s.Start();
        };
    }

    void IWatcher.Watch(IScenario scenario)
    {
        Watch((TWatch)scenario);
    }
}