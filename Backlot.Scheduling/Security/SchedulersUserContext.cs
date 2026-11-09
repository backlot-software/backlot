#nullable enable
using Backlot.Core.Security;

namespace Backlot.Scheduling.Security;

/// <summary>
/// User context of the scheduler process: finds due schedules, plays their scenarios and saves run state.
/// Schedules must grant <see cref="Name"/> read + write + execute.
/// This user context acts like a super admin.
/// </summary>
internal sealed class SchedulersUserContext : IUserContext
{
    public const string Name = "scheduler";

    public string AuthScheme => "Internal";

    public Task Intialize(string token)
    {
        throw new ArgumentException($"The '{nameof(SchedulersUserContext)}' is for internal use and can not be initialized by a token.");
    }

    public string UserName => Name;

    public IDictionary<string, string> Claims => new Dictionary<string, string>() { {"sub", UserName}, { "groups", string.Join(",", Groups) } };

    public string[] Groups => [Access.Admin, Access.Everyone];

    public bool IsInGroup(string groupname) => false;

    public bool IsAuthenticated => true;
}
