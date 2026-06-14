using Orleans;

namespace Turbo.Primitives.Catalog.Snapshots;

[GenerateSerializer, Immutable]
public sealed record UpcomingLtdSnapshot
{
    [Id(0)]
    public required int SeriesId { get; init; }

    [Id(1)]
    public required int FurniDefinitionId { get; init; }

    [Id(2)]
    public required int TotalQuantity { get; init; }

    [Id(3)]
    public required int RemainingQuantity { get; init; }

    [Id(4)]
    public required int Price { get; init; }

    [Id(5)]
    public required int CurrencyType { get; init; }
}
