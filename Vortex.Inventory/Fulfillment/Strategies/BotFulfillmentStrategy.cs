using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Inventory.Fulfillment.Strategies;

/// <summary>
/// A bot product. The catalogue row carries the figure and the defaults; the purchase's own extra
/// param carries the name the buyer typed.
/// </summary>
/// <remarks>
/// A bot with no figure is skipped and logged rather than granted blank. A blank bot is an item in
/// somebody's hand that renders as nothing and cannot be placed, which is worse than an item they
/// never received and can ask about.
/// </remarks>
internal sealed class BotFulfillmentStrategy(ILogger logger) : IFulfillmentStrategy
{
    public IReadOnlyList<ProductType> Handles { get; } = [ProductType.Robot];

    public void Contribute(in FulfillmentRequest request, FulfillmentPlanBuilder into)
    {
        BotCreateRequest? bot = BotProductReader.TryRead(
            request.Product.ExtraParam,
            request.PurchaseExtraParam
        );

        if (bot is null)
        {
            logger.LogWarning(
                "Catalog product {ProductId} is a Robot but its extra param '{ExtraParam}' carries "
                    + "no figure; skipping the grant.",
                request.Product.Id,
                request.Product.ExtraParam
            );

            return;
        }

        into.AddBot(bot);
    }
}
