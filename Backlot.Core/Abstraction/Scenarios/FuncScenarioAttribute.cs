using System;
using System.Linq;
using Newtonsoft.Json;

namespace Backlot.Core.Abstraction.Scenarios
{
    /// <summary>
    /// Attribute which you can use to define the scenario reference of an implemented scenario.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FuncScenarioAttribute : Attribute, IScenarioInfo
    {
        [JsonIgnore] public override object TypeId => base.TypeId;

        /// <summary>
        /// INTERNAL: Parameterless constructor, only used by deserialization / factories.
        /// </summary>
        // ReSharper disable once UnusedMember.Global : Is used by the builders
        [Obsolete("INTERNAL: Parameterless constructor, only used by deserialization / factories.")]
        public FuncScenarioAttribute()
        {
        }

        /// <summary>
        /// FuncScenario specify Type and name
        /// </summary>
        /// <param name="trole"></param>
        /// <param name="tresult"></param>
        /// <param name="name"></param>
        /// <param name="access"></param>
        public FuncScenarioAttribute(Type trole, Type tresult, string name, string[] access = null)
        {
            Name = name;
            TRole = trole;
            TResult = tresult;
            Tags = Enumerable.Empty<string>().ToArray();
            Access = access ?? [];
        }

        public FuncScenarioAttribute(Type trole,
            Type tresult,
            string name,
            string[] tags,
            string[] access = null) : this(trole, tresult, name, access)
        {
            Tags = tags;
        }

        /// <summary>
        /// Defined name of the refering scenario; this also is the name how to reach this function from external calls like a webservice endpoint.
        /// </summary>
        public string Name { get; }

        public Type TRole { get; }
        public Type TResult { get; }
        public Type ScenarioType => typeof(IFuncScenario);
        public string[] Tags { get; }
        public string ConfigurationPath => $"Func.{Name}";
        public string[] Access { get; }
    }
}

