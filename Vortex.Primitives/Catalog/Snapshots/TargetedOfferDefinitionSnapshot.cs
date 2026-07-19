using System;
using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

/// <summary>A cached targeted-offer definition: its display/price fields plus the grantable bundle.</summary>
[GenerateSerializer, Immutable]
public sealed record TargetedOfferDefinitionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Identifier { get; init; }

    [Id(2)]
    public required int OfferType { get; init; }

    [Id(3)]
    public required string Title { get; init; }

    [Id(4)]
    public required string Description { get; init; }

    [Id(5)]
    public required string ImageUrl { get; init; }

    [Id(6)]
    public required string IconImageUrl { get; init; }

    [Id(7)]
    public required string ProductCode { get; init; }

    [Id(8)]
    public required int PriceInCredits { get; init; }

    [Id(9)]
    public required int PriceInActivityPoints { get; init; }

    [Id(10)]
    public required int ActivityPointType { get; init; }

    [Id(11)]
    public required int PurchaseLimit { get; init; }

    [Id(12)]
    public required DateTime? ExpiresAt { get; init; }

    [Id(13)]
    public required int SortOrder { get; init; }

    [Id(14)]
    public required ImmutableArray<TargetedOfferProductSnapshot> Products { get; init; }
}
