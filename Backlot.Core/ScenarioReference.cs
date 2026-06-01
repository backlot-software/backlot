
// ReSharper disable InconsistentNaming : TExtensions are allowed for internal use here.

namespace Backlot.Core
{ 
    /// <summary>
    /// unique reference to a a scenario.
    /// </summary>
    public sealed class ScenarioReference
    {
        /// <summary>
        /// "unique" name refering to the scenario.
        /// ScenarioReferences do support Named instances as well in the meaning that
        /// the second part of the name does define which configurations are loaded.
        /// {scenarioname}.{configurationpath}
        /// When no configuration part is used defaults are loaded.
        /// </summary>
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ScenarioReference sr)
            {
                return sr.Name == Name;
            }

            return false;
        }
    }
}