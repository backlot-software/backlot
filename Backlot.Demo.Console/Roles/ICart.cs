using Backlot.Core;
using Backlot.Core.Json;
using Newtonsoft.Json;

namespace Backlot.Demo.Console.Roles;

/// <summary>
/// Shoppinglist can be used to initate an ICart or f.e. for a wishlist.
/// </summary>
public interface IShoppingList : IRole, IPersist
{
    IEnumerable<ILineItem>? LineItems { get; }
}

/// <summary>
/// A Shoppingcart which can be checked / payed for.
/// </summary>
public interface ICart : IShoppingList
{
    /// <summary>
    /// Coupons can be used within promotions.
    /// </summary>
    IEnumerable<string>? Coupons { get; }

    /// <summary>
    /// Promotions are scenarios returning a discount which is used during the calculation scenario.
    /// </summary>
    IEnumerable<ScenarioReference>? Promotions { get; }

    ScenarioReference? Shipment { get; }

    /// <summary>
    /// Total calculation of all promotions
    /// </summary>
    [Calculated]
    IMoney CalculatedPromotionTotal { get; set; }
        
    /// <summary>
    /// Total of shipment costs based on the Shipment scenarioreference
    /// </summary>
    [Calculated]
    IMoney CalculatedShipmentCost { get; set; }
        
    /// <summary>
    /// The same as calculated total, but for internal use only.
    /// The calculatedSubTotal contains the sum during the calculation process and can differ based on where the calculation currently is in the execution path of calculation.
    /// </summary>
    [JsonIgnore]
    [Calculated]
    IMoney CalculatedSubTotal { get; set; }
        
    /// <summary>
    /// The final calculated total.
    /// </summary>
    [Calculated]
    IMoney CalculatedTotal { get; set; }
        
    //todo: wip:
    /// <summary>
    /// The final calculated total.
    /// </summary>
    // [Calculated]
    // IMoney CalculatedTaxTotal { get; set; }
        
    bool TaxIncluded { get; set; }
}