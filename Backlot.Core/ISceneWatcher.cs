namespace Backlot.Core
{
    /// <summary>
    /// Can be used to watch every event of a certain scenario
    /// </summary>
    /// <typeparam name="TScenario"></typeparam>
    public interface ISceneWatcher<in TScenario> : IWatcher<TScenario>
        where TScenario : IScenario
    {
      
    }
}