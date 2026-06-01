using System;
using System.Linq;
using Castle.DynamicProxy.Internal;
using Newtonsoft.Json;

namespace Backlot.Core.Abstraction.Scenarios
{
    /// <summary>
    /// Attribute which you can use to define the scenario reference of an implemented scenario.
    /// This attribute is needed if you like to make a scenario accessable via web.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScenarioAttribute : Attribute, IScenarioInfo
    {
        [JsonIgnore] public override object TypeId => base.TypeId;

        /// <summary>
        /// INTERNAL: Parameterless constructor, only used by deserialization / factories.
        /// </summary>
        // ReSharper disable once UnusedMember.Global : Is used by the builders
        [Obsolete("INTERNAL: Parameterless constructor, only used by deserialization / factories.")]
        public ScenarioAttribute()
        {
        }

        public ScenarioAttribute(Type type, string[] tags = null, string[] access = null) 
        {
            var typedScenarioType = type.GetAllInterfaces()
                .FirstOrDefault(t => t.Name == typeof(IScenario<,>).Name);
            
            TRole = typedScenarioType?.GenericTypeArguments[0];
            TResult = typedScenarioType?.GenericTypeArguments[1];
            ScenarioType = type;
            
            if (typedScenarioType == null)
            {
                var objectScenarioType = type.GetAllInterfaces()
                    .FirstOrDefault(t => t.Name == typeof(IScenario<>).Name);
                
                TRole = objectScenarioType?.GenericTypeArguments[0];
            }

            TRole ??= typeof(object);
            TResult ??= typeof(object);

            Name = type.Name;
            ConfigurationPath = type.FullName;

            if (tags == null && type.Namespace != null)
            {
                Tags = [type.Namespace.Split(".").Last()];
            }
            else
            {
                Tags = tags ?? [];    
            }

            Access = access ?? [];
        }


        public string Name { get; }
        public Type TRole { get; }
        public Type TResult { get; }
        public Type ScenarioType { get; }
        
        /// <summary>
        /// Scenario groups used for querying through scenarios
        /// </summary>
        public string[] Tags { get; }
        public string ConfigurationPath { get; }
        public string[] Access { get; }
    }
}
