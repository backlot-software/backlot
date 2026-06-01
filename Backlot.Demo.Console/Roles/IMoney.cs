using Backlot.Core;

namespace Backlot.Demo.Console.Roles;

public interface IMoney : IRole
{
    decimal Value { get; set; }
    string Currency { get; set; }
}

public static class MoneyInitialization
{
    public static IMoney Initialize(IMoney money, object actor)
    {
        money.Currency ??= "EUR"; //todo: get default from settings.
        return money;
    }
}