using System;

namespace Backlot.Core.Abstraction.Scenarios;

/// <summary>
/// Allows parameters of type IRole where Role.IsNull() is true.
/// ---
/// AWARE: Keep in mind it does not overrule any other validation for the specific role.
/// When you have required fields and the Role becomes pulicly available as a property,
/// you need to take all requirements (such as required Uid's) into account to allow the scenario to execute.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class NullAllowedAttribute : Attribute
{
}