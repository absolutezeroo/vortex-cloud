using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Players;

/// <summary>One saved avatar-editor wardrobe slot, round-tripped to the client on GetWardrobe.</summary>
[GenerateSerializer, Immutable]
public sealed record PlayerWardrobeOutfitSnapshot
{
    [Id(0)]
    public required int SlotId { get; init; }

    [Id(1)]
    public required string Figure { get; init; }

    [Id(2)]
    public required string Gender { get; init; }
}
