using Orleans;

namespace Vortex.Primitives.Catalog.Snapshots;

/// <summary>One product in a targeted offer's bundle: its display code and the furniture actually
/// granted on purchase (if any), <see cref="Quantity"/> times.</summary>
[GenerateSerializer, Immutable]
public sealed record TargetedOfferProductSnapshot
{
    [Id(0)]
    public required string ProductCode { get; init; }

    [Id(1)]
    public required int? FurnitureDefinitionId { get; init; }

    [Id(2)]
    public required int Quantity { get; init; }
}
