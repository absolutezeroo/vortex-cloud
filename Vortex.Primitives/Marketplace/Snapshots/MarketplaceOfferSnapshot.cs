using Orleans;

namespace Vortex.Primitives.Marketplace.Snapshots;

[GenerateSerializer, Immutable]
public sealed record MarketplaceOfferSnapshot
{
    [Id(0)]
    public required int OfferId { get; init; }

    [Id(1)]
    public required int SpriteId { get; init; }

    [Id(2)]
    public required int FurnitureType { get; init; }

    [Id(3)]
    public required string? ExtraData { get; init; }

    [Id(4)]
    public required int Price { get; init; }

    [Id(5)]
    public required int AvgPrice { get; init; }

    [Id(6)]
    public required int OfferCount { get; init; }

    [Id(7)]
    public required int ExpiresIn { get; init; }

    [Id(8)]
    public required int Status { get; init; }

    [Id(9)]
    public required int CreditsOwed { get; init; }
}
