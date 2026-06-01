using Backlot.Core;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Security;
using Newtonsoft.Json.Linq;

namespace Backlot.Defaults.Scenarios.Authentication
{
    /// <summary>
    /// Creates an access token with the same jti as the "parent" refresh token.
    /// Access tokens are ideally short lived and used to access resources using the IUserContext.
    /// </summary>
    [Scenario(typeof(WhoAmI), access: [Access.Everyone])]
    public class WhoAmI : DirectorScenario<WhoAmI, object>
    {
        // composition
        /// <summary>
        /// A free Info object which is application dependend. It can load settings from a file or add extra context to the result of this scenario.
        /// </summary>
        public Func<Task<JObject>> GetInfo { get; set; } = () => Task.FromResult(new JObject());

        public WhoAmI(IDirector role) : base(role)
        {

        }

        protected override async Task<dynamic> ExecAsync()
        {
            return new
            {
                UserContext.Current.UserName,
                UserContext.Current.Groups,
                Info =  await GetInfo()
            };
        }
    }
}