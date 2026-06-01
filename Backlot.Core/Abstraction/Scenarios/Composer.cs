using System;

namespace Backlot.Core.Abstraction.Scenarios
{
    public interface IComposer
    {
        void Compose(IScenario scenario);
    }
    
    /// <summary>
    /// Scenario composition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Composer<T> : IComposer
        where T: IScenario
    {
        public Composer(Action<T> composition)
        {
            Compose = composition;
        }

        /// <summary>
        /// Compose all open ends for the given scenario
        /// </summary>
        private Action<T> Compose { get; }

        void IComposer.Compose(IScenario scenario)
        {
            Compose((T)scenario);
        }
    }
}