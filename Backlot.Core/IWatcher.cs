namespace Backlot.Core;

/// <summary>
/// Default watcher interface used by Binge and Scene watchers.
/// </summary>
/// <typeparam name="TScenario"></typeparam>
public interface IWatcher<in TScenario> : IWatcher
    where TScenario : IScenario
{
    /// <summary>
    /// Start watching
    /// </summary>
    /// <param name="scenario"></param>
    public void Watch(TScenario scenario);
}

public interface IWatcher
{
    /// <summary>
    /// Start watching
    /// </summary>
    /// <param name="scenario"></param>
    public void Watch(IScenario scenario);
}