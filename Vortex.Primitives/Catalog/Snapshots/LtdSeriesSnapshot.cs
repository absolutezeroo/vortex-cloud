using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record LtdSeriesSnapshot
{
    [Id(0)]
    public required int SeriesId { get; init; }

    [Id(1)]
    public required int CatalogProductId { get; init; }

    [Id(2)]
    public required int TotalQuantity { get; init; }

    [Id(3)]
    public required int RemainingQuantity { get; init; }

    [Id(4)]
    public required int CostCredits { get; init; }

    [Id(5)]
    public required int RaffleWindowSeconds { get; init; }

    [Id(6)]
    public required bool IsActive { get; init; }

    [Id(7)]
    public required bool HasRaffleFinished { get; init; }
}
