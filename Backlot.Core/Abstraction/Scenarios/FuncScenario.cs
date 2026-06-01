using System;
using System.Reflection;

namespace Backlot.Core.Abstraction.Scenarios
{
    /// <summary>
    /// The Method you like to wrap.
    /// </summary>
    public interface IFuncScenario : IScenario
    {
        MethodInfo Func { get; }
    }

    /// <summary>
    /// Wrapper for static functions to be executed and build like scenarios
    /// Funcs on themselves do not have context, if you like to have context implement a class inheriting from Scenario.
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public class FuncScenario<TRole, TResult> : Scenario<TRole, TResult>, IFuncScenario
        where TRole : IRole
    {
        private readonly Func<TRole, TResult> _func;

        public FuncScenario(TRole role, Func<TRole, TResult> func) 
            : this(role, func, func.GetMethodInfo()) { }

        public FuncScenario(TRole role, Func<TRole, TResult> func, MethodInfo funcMethod)
        {
            _func = func;
            Func = funcMethod;
            
            Initialize(this, role, Director);
        }
        
        protected override TResult Exec()
        {
            return _func(Role);
        }

        public MethodInfo Func { get; }
    }
}