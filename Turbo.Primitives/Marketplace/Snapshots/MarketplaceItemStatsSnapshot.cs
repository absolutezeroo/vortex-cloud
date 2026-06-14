using System.Collections.Generic;
using Orleans;

namespace Turbo.Primitives.Marketplace.Snapshots;

[GenerateSerializer, Immutable]
public sealed record MarketplaceItemStatsSnapshot
{
    [Id(0)]
    public required int SpriteId { get; init; }

    [Id(1)]
    public required int AvgPrice { get; init; }

    [Id(2)]
    public required int OfferCount { get; init; }

    [Id(3)]
    public required List<MarketplaceHistoryEntry> History { get; init; }

    [Id(4)]
    public required int MinSellValue { get; init; }

    [Id(5)]
    public required int MaxSellValue { get; init; }
}
