using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One targeted offer with its bundle and its lifetime totals.</summary>
public sealed record TargetedOfferDetail(
    int Id,
    string Identifier,
    int OfferType,
    string Title,
    string Description,
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
    int BuyerCount,
    int TotalPurchases,
    IReadOnlyList<TargetedOfferProduct> Products
);
