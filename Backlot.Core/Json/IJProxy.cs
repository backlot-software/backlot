using Newtonsoft.Json.Linq;

namespace Backlot.Core.Json
{
    /// <summary>
    /// INTERNAL: A proxied json origin.
    /// -- For internal use of Backlot.Core only.
    /// </summary>
    public interface IJProxy
    {
        /// <summary>
        /// Use with caution; Typed / JObject representation of the Actor.
        /// Is a candidate to change in the future.
        /// </summary>
        [global::Newtonsoft.Json.JsonIgnore]
        JObject JActor { get; }
    }
}