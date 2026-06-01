using Backlot.Core;

namespace Backlot.Testing.Core;

/// <summary>
/// Representing a test role using another role as a property
/// </summary>
public interface IOrder : IRole
{
    public string Name { get; set; }
    public IMoney Total { get; set; }
}

public interface IPersistedOrder : IPersist
{
    public IMoney Total { get; set; }
}

public interface IMoney : IRole
{
    decimal Value { get; set; }
    string Currency { get; set; }
}

/// <summary>
/// Role initialization like it is done in f.e. Versla.
/// </summary>
public static class MoneyInitialization
{
    public static IMoney Initialize(IMoney money, object actor)
    {
        money.Currency ??= "EUR"; //todo: get default from settings.
        return money;
    }
}