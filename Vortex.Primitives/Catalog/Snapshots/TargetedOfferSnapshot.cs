using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

/// <summary>
/// Wire-ready view of a targeted offer for a specific player, mapping 1:1 to the client's
/// TargetedOffer payload. <see cref="PurchaseLimit"/> is the number of purchases the player has left
/// and <see cref="ExpirationSeconds"/> is the seconds until the offer expires (0 = no expiry).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record TargetedOfferSnapshot
{
    [Id(0)]
    public required int TrackingState { get; init; }

    [Id(1)]
    public required int Id { get; init; }

    [Id(2)]
    public required string Identifier { get; init; }

    [Id(3)]
    public required string ProductCode { get; init; }

    [Id(4)]
    public required int PriceInCredits { get; init; }

    [Id(5)]
    public required int PriceInActivityPoints { get; init; }

    [Id(6)]
    public required int ActivityPointType { get; init; }

    [Id(7)]
    public required int PurchaseLimit { get; init; }

    [Id(8)]
    public required int ExpirationSeconds { get; init; }

    [Id(9)]
    public required string Title { get; init; }

    [Id(10)]
    public required string Description { get; init; }

    [Id(11)]
    public required string ImageUrl { get; init; }

    [Id(12)]
    public required string IconImageUrl { get; init; }

    [Id(13)]
    public required int OfferType { get; init; }

    [Id(14)]
    public required ImmutableArray<string> SubProductCodes { get; init; }
}
