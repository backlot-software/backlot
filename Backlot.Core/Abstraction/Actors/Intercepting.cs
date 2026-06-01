using Castle.DynamicProxy;

namespace Backlot.Core.Abstraction.Actors;

/// <summary>
/// Use singletons for proxy generation to improve performance and reduce memory footprint.
/// </summary>
internal static class ProxyGeneration
{
    /// <summary>
    /// Singleton for proxy generation.
    /// </summary>
    internal static readonly ProxyGenerator Generator = new();
    
    /// <summary>
    /// Singleton for proxy generation.
    /// </summary>
    internal static readonly ProxyGenerationOptions Options = new();
}