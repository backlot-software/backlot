using Backlot.Studio.Models.Api;

namespace Backlot.Studio.Services;

public interface IBacklotApiClient
{
    Task<bool> IsAuthenticatedAsync();
    Task<object?> WhoAmIAsync();
    Task<IEnumerable<ScenarioItem>?> GetScenariosAsync();
}
