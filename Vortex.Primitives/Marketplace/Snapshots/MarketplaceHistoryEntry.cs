using Orleans;

namespace Vortex.Primitives.Marketplace.Snapshots;

[GenerateSerializer, Immutable]
public sealed record MarketplaceHistoryEntry
{
    [Id(0)]
    public required int Day { get; init; }

    [Id(1)]
    public required int Month { get; init; }

    [Id(2)]
    public required int Year { get; init; }

    [Id(3)]
    public required int AvgPrice { get; init; }

    [Id(4)]
    public required int Count { get; init; }
}
