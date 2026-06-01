using System;
// ReSharper disable InconsistentNaming

namespace Backlot.Core.Abstraction.Scenarios;

public interface IScenarioInfo
{
    string Name { get; }
    
    /// <summary>
    /// The main role for this scenario
    /// Like to get all roles use the exention method TRoles()
    /// </summary>
    Type TRole { get; }
    
    Type TResult { get; }
    
    Type ScenarioType { get; }
    
    /// <summary>
    /// Group scenarios for querying.
    /// </summary>
    string[] Tags { get; } 
    
    /// <summary>
    /// Configuration path used in .json configs and or azure configuration files.
    /// </summary>
    string ConfigurationPath { get; }

    /// <summary>
    /// Add groups and or specific users which are allowed to execute this scenario.
    /// </summary>
    string[] Access { get; }
}

/// <summary>
/// The internal class no scenario is used when no scenario attribute is given on top of the function or class scenario.
/// The scenario.info class will than calculate a "default" scenarioinfo object.
/// </summary>
/// <typeparam name="TR"></typeparam>
/// <typeparam name="TS"></typeparam>
internal class NoScenarioInfo<TR, TS> : IScenarioInfo
    where TR : IRole
{
    public NoScenarioInfo(string name, Type scenarioType, string[] tags, string configurationPath)
    {
        Name = name;
        ScenarioType = scenarioType;
        Tags = tags;
        ConfigurationPath = configurationPath;
    }

    public string Name { get; }
    public Type TRole => typeof(TR);
    public Type TResult => typeof(TS);
    public Type ScenarioType { get; }
    public string[] Tags { get; }
    public string ConfigurationPath { get; }
    public string[] Access => []; // empty is no one has access from the outside;
}