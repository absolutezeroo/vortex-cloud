using System;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the dashboard's targeted-offer admin operations. Each carries a mandatory
/// <c>Reason</c> (audited) like every other operation request. The offer's bundle products are
/// managed with the separate product requests below.
/// </summary>
public sealed record CreateTargetedOfferRequest(
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
);

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
);

public sealed record DeleteTargetedOfferRequest(int OfferId, string Reason);

public sealed record CreateTargetedOfferProductRequest(
    int OfferId,
    string ProductCode,
    int? FurnitureDefinitionId,
    int Quantity,
    string Reason
);

public sealed record UpdateTargetedOfferProductRequest(
    int ProductId,
    string ProductCode,
    int? FurnitureDefinitionId,
    int Quantity,
    string Reason
);

public sealed record DeleteTargetedOfferProductRequest(int ProductId, string Reason);
