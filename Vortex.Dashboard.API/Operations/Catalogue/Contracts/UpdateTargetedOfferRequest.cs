using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateTargetedOfferRequest(
    int OfferId,
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
    bool Active,
    int SortOrder,
    string Reason
) : IReasonedRequest;
