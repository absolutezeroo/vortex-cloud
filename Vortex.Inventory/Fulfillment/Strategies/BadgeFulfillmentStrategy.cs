using System.Collections.Generic;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Inventory.Fulfillment.Strategies;

/// <summary>
/// A badge product: the code, once, whatever the quantity.
/// </summary>
/// <remarks>
/// Deliberately not multiplied. A badge is held or not held, so promising five would promise four
/// rows the inventory refuses anyway. A product with no code promises nothing — that guard used to
/// sit on the dispatch, one place further from the rule than it belonged.
/// </remarks>
internal sealed class BadgeFulfillmentStrategy : IFulfillmentStrategy
{
    public IReadOnlyList<ProductType> Handles { get; } = [ProductType.Badge];

    public void Contribute(in FulfillmentRequest request, FulfillmentPlanBuilder into)
    {
        if (string.IsNullOrWhiteSpace(request.Product.ExtraParam))
        {
            return;
        }

        into.AddBadge(request.Product.ExtraParam);
    }
}
