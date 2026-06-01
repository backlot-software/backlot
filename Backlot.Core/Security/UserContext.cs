using Backlot.Core.DependencyInjection;

namespace Backlot.Core.Security;

public static class UserContext
{
    public static IUserContext Current => ServiceLocator.Get<IUserContext>();
}