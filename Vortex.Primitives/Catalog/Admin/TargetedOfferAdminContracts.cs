using System;

namespace Vortex.Primitives.Catalog.Admin;

/// <summary>
/// Create/update specs for the dashboard's targeted-offer admin surface. The offer's bundle
/// products are managed independently (see the product specs) so an offer can be re-priced without
/// touching its grants. Reuses <see cref="CatalogAdminResult"/> for the outcome shape.
/// </summary>
public sealed record TargetedOfferCreateSpec(
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
    int SortOrder
);

public sealed record TargetedOfferUpdateSpec(
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
    int SortOrder
);

/// <summary>
/// <paramref name="OfferId"/> is set once at creation only — a product belongs to exactly one
/// offer and re-parenting it is out of scope for this admin surface.
/// </summary>
public sealed record TargetedOfferProductCreateSpec(
    int OfferId,
    string ProductCode,
    int? FurnitureDefinitionId,
    int Quantity
);

public sealed record TargetedOfferProductUpdateSpec(
    string ProductCode,
    int? FurnitureDefinitionId,
    int Quantity
);
