using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>One kind of item that can be minted into a token.</summary>
[GenerateSerializer, Immutable]
public sealed record MintableItemTypeSnapshot
{
    [Id(0)]
    public required int ItemTypeId { get; init; }

    [Id(1)]
    public required int StartTime { get; init; }

    [Id(2)]
    public required int EndTime { get; init; }

    [Id(3)]
    public required bool RegionLocked { get; init; }

    [Id(4)]
    public required int Price { get; init; }

    [Id(5)]
    public required bool LimitedEdition { get; init; }

    /// <summary>A short on the wire, last -- not an int like the other numbers here.</summary>
    [Id(6)]
    public required short ItemType { get; init; }
}
