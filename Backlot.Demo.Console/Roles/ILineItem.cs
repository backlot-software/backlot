using Backlot.Core;
using Backlot.Core.Json;

namespace Backlot.Demo.Console.Roles;

public interface ILineItem : IRole //not an IPersist because not operation on its own.
{
    string Uid { get; set; }
    string Name { get; set; }
        
    decimal Quantity { get; }
    string Description { get; }

    /// <summary>
    /// Calculated line price, when discounts are valid for this line, this price shows the discounted price.
    /// Costs for shipments or certain payment methods, need to be a lineitem as well.
    /// </summary>
    [Calculated]
    IMoney LinePrice { get; set; }

    /// <summary>
    /// The price for off one specific item, no discounts in consideration.
    /// </summary>
    IMoney ItemPrice { get; }

    bool TaxIncluded { get; set; }

    IEnumerable<string> PriceGroups { get; set; }
}

public static class LineItemInitialization
{
    public static ILineItem Initialize(ILineItem item, object actor)
    {
        item.PriceGroups ??= Enumerable.Empty<string>().ToArray();

        return item;
    }
}