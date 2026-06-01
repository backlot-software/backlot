using System;

namespace Backlot.Core.Abstraction.Roles
{
    /// <summary>
    /// Role instructor for an actor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Instructor<T>
        where T: IRole
    {
        public Instructor(Func<T, object, T> instruction)
        {
            Instruct = instruction;
        }

        /// <summary>
        /// Execute an optional instruction during role building.
        /// An instruct functions always returns the role.
        /// param 1: The role
        /// param 2: The actor
        /// </summary>
        public Func<T,object, T> Instruct { get; }
        
        private int? _priority;

        public int Priority
        {
            get => _priority ?? 0;
            set => _priority = value;
        }
    }
}