using System.Collections.Generic;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;

namespace Backlot.Testing.Shared.TestModels;

public interface ICard : IPersist
{
    string CardCode { get; set; }
    string CustomerName { get; set; }
}

/// <summary>
/// Initialize card uid when not "mapped"
/// </summary>
public static class CardInitialization
{
    public static ICard Initialize(ICard card, object actor)
    {
        if (card is IProxiedRole proxy && !string.IsNullOrWhiteSpace(card.CardCode))
        {
            proxy.Referrers = () => new Dictionary<string, string>
            {
                {nameof(ICard.Uid), nameof(ICard.CardCode)},
            };
        }
        
        // no need to check uid for other type of objects, because it's a required property


        return card;
    }
}