using System;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One targeted offer.
/// </summary>
/// <param name="Expired">Past its date. An expired offer can still be active, which is exactly the
/// state that looks configured and sells nothing.</param>
/// <param name="BuyerCount">Distinct players who bought at least once; <paramref name="TotalPurchases"/>
/// counts the buys, which is higher wherever the limit allows more than one.</param>
public sealed record TargetedOfferRow(
    int Id,
    string Identifier,
    int OfferType,
    string Title,
    string ImageUrl,
    string IconImageUrl,
    string ProductCode,
    int PriceInCredits,
    int PriceInActivityPoints,
    int ActivityPointType,
    int PurchaseLimit,
    DateTime? ExpiresAt,
    bool Expired,
    bool Active,
    int SortOrder,
    int ProductCount,
    int BuyerCount,
    int TotalPurchases
);
