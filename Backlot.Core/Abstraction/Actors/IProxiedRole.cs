using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Backlot.Core.Abstraction.Actors
{
    /// <summary>
    /// All actors that are proxied as a role by Backlot.Core.Abstraction.Actors.Acting..
    /// </summary>
    public interface IProxiedRole
    {
        /// <summary>
        /// The original actor that is proxied.
        /// When actors do have a respresentation of the role property which is set, the actor is updated by the interceptor when "set" is called.
        /// </summary>
        [JsonIgnore]
        object Actor { get; }

        /// <summary>
        /// The actual type of the Role this proxy is representing.
        /// </summary>
        /// <returns></returns>
        Type ProxiedType();
        
        /// <summary>
        /// Dictionary of properties which are referring to an alias or expression
        /// key: role propertyname
        /// value: what the interceptor needs to have to get the value from the actor. This can be:
        /// - the actual property name used within the underlying actor
        /// - or an expression following the regex pattern. ^(?.):(?.*)$ Acting.RefererExpressionEnginePattern
        /// </summary>
        /// <returns></returns>
        [JsonIgnore]
        Func<IDictionary<string, string>> Referrers { get; set; }

        /// <summary>
        /// Is the representation of the actor null or not.
        /// </summary>
        /// <returns></returns>
        bool IsNull();

        /// <summary>
        /// INTNERAL: The interceptor that is used to intercept the calls to the actor.
        /// Advanced usage only, and advisable to not use in none core libraries.
        /// Signature and use can be changed without any notice.
        /// </summary>
        [JsonIgnore]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IProxyInterceptor Interceptor { get; }

        /// <summary>
        /// All skills of the current proxied role includes both; previous and current skillset.
        /// </summary>
        /// <returns></returns>
        string[] Skills();
    }
}
