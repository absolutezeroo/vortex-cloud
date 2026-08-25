using System;
using System.Collections.Generic;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Inventory.Fulfillment.Strategies;

/// <summary>
/// An avatar effect. <c>ExtraParam</c> encodes it as <c>effectId</c>,
/// <c>effectId:durationSeconds</c>, or <c>effectId:durationSeconds:subType</c>; duration 0 or absent
/// means permanent. One grant per copy.
/// </summary>
/// <remarks>
/// A malformed param promises nothing rather than throwing. Throwing would be defensible — this runs
/// before the debit, so nothing would need compensating — but an effect id that will not parse is a
/// catalogue row somebody mistyped, and refusing the whole purchase over one product of a bundle
/// makes the buyer pay for it.
/// </remarks>
internal sealed class EffectFulfillmentStrategy : IFulfillmentStrategy
{
    public IReadOnlyList<ProductType> Handles { get; } = [ProductType.Effect];

    public void Contribute(in FulfillmentRequest request, FulfillmentPlanBuilder into)
    {
        if (string.IsNullOrWhiteSpace(request.Product.ExtraParam))
        {
            return;
        }

        string[] fx = request.Product.ExtraParam.Split(':');

        if (!int.TryParse(fx[0], out int effectId) || effectId <= 0)
        {
            return;
        }

        int duration = fx.Length > 1 && int.TryParse(fx[1], out int d) ? d : 0;
        int subType = fx.Length > 2 && int.TryParse(fx[2], out int s) ? s : 0;
        int copies = request.Quantity * Math.Max(1, request.Product.Quantity);

        for (int i = 0; i < copies; i++)
        {
            into.AddEffect(new PlannedEffect(effectId, subType, duration));
        }
    }
}
