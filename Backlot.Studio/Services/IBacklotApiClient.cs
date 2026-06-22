namespace Backlot.Studio.Services;

// Phase 2 will add GetScenariosAsync — keep this interface minimal and clean
// so that addition requires only adding a method signature.
public interface IBacklotApiClient
{
    Task<bool> IsAuthenticatedAsync();
    Task<object?> WhoAmIAsync();
}
