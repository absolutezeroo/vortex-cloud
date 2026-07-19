using System;
using System.Collections.Immutable;
using System.Linq;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Catalog.TargetedOffers;

/// <summary>
/// Pure mapping/eligibility helpers for targeted offers, kept free of Orleans/DB so the
/// remaining-purchases, expiry and can-purchase rules can be unit-tested.
/// </summary>
public static class TargetedOfferMapper
{
    /// <summary>
    /// Builds the wire snapshot for a player: <c>PurchaseLimit</c> becomes the purchases the player
    /// has left and <c>ExpirationSeconds</c> the seconds until expiry (0 = none).
    /// </summary>
    public static TargetedOfferSnapshot ToWire(
        TargetedOfferDefinitionSnapshot definition,
        int purchaseCount,
        int trackingState,
        DateTime now
    )
    {
        return new TargetedOfferSnapshot
        {
            TrackingState = trackingState,
            Id = definition.Id,
            Identifier = definition.Identifier,
            ProductCode = definition.ProductCode,
            PriceInCredits = definition.PriceInCredits,
            PriceInActivityPoints = definition.PriceInActivityPoints,
            ActivityPointType = definition.ActivityPointType,
            PurchaseLimit = RemainingPurchases(definition, purchaseCount),
            ExpirationSeconds = ExpirationSeconds(definition, now),
            Title = definition.Title,
            Description = definition.Description,
            ImageUrl = definition.ImageUrl,
            IconImageUrl = definition.IconImageUrl,
            OfferType = definition.OfferType,
            SubProductCodes = [.. definition.Products.Select(p => p.ProductCode)],
        };
    }

    /// <summary>Purchases the player has left (0 once the limit is reached; limit &lt;= 0 = unlimited).</summary>
    public static int RemainingPurchases(
        TargetedOfferDefinitionSnapshot definition,
        int purchaseCount
    ) =>
        definition.PurchaseLimit <= 0
            ? int.MaxValue
            : Math.Max(0, definition.PurchaseLimit - purchaseCount);

    /// <summary>Seconds until the offer expires, clamped at 0; 0 when the offer has no expiry.</summary>
    public static int ExpirationSeconds(TargetedOfferDefinitionSnapshot definition, DateTime now)
    {
        if (definition.ExpiresAt is not DateTime expiresAt)
        {
            return 0;
        }

        double seconds = (expiresAt - now).TotalSeconds;
        return seconds <= 0 ? 0 : (int)seconds;
    }

    /// <summary>Whether the player can still purchase this offer at <paramref name="now"/>.</summary>
    public static bool CanPurchase(
        TargetedOfferDefinitionSnapshot definition,
        int purchaseCount,
        DateTime now
    )
    {
        if (definition.ExpiresAt is DateTime expiresAt && expiresAt <= now)
        {
            return false;
        }

        return definition.PurchaseLimit <= 0 || purchaseCount < definition.PurchaseLimit;
    }
}
