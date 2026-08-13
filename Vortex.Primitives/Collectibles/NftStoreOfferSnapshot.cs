using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// One offer on the collectibles shop tab.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftStoreOfferSnapshot
{
    [Id(0)]
    public required string ProductCode { get; init; }

    /// <summary>Priced in emeralds, not credits — this tab spends the collectibles currency.</summary>
    [Id(1)]
    public required int EmeraldPrice { get; init; }

    [Id(2)]
    public required bool IsFeatured { get; init; }

    [Id(3)]
    public required bool IsLimited { get; init; }

    [Id(4)]
    public required int MintLimit { get; init; }

    [Id(5)]
    public required int MintedCount { get; init; }

    /// <summary>The collectible itself — the base product struct, read here without the amount
    /// field the collections list adds to it.</summary>
    [Id(6)]
    public required CollectibleProductItemSnapshot ProductInfo { get; init; }
}
