using System;

namespace Backlot.Core.Abstraction.Actors;

public interface IExpressionEngine
{
    /// <summary>
    /// The 'name' of the engine, which is always one character.
    /// </summary>
    char Engine { get; }
    /// <summary>
    /// Get a value based on an expression reference
    /// </summary>
    /// <param name="expression">The expression content</param>
    /// <param name="type">The return type</param>
    /// <param name="actor">The actor of the role</param>
    /// <returns>The value of the defined type</returns>
    object Execute(string expression, Type type, object actor);
}
public interface IExpressionEngine<T> : IExpressionEngine
{
    T Execute(string expression, object actor);
}