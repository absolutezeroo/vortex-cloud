using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Habbicons.Snapshots;

/// <summary>A collection as one player sees it, with their own progress folded in.</summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconShopCollectionSnapshot
{
    [Id(0)]
    public required int CollectionId { get; init; }

    [Id(1)]
    public required string Code { get; init; }

    /// <summary>Whether this player owns every ordinary entry. Derived, never stored.</summary>
    [Id(2)]
    public required bool Completed { get; init; }

    /// <summary>The bonus Habbicon's id, or 0 when the set has none.</summary>
    [Id(3)]
    public required int RewardHabbiconId { get; init; }

    /// <summary>The player's state on the bonus Habbicon (<see cref="HabbiconState.Claimable"/> once earned).</summary>
    [Id(4)]
    public required HabbiconState RewardState { get; init; }

    [Id(5)]
    public required int PriceCredits { get; init; }

    [Id(6)]
    public required int PriceActivityPoints { get; init; }

    [Id(7)]
    public required int ActivityPointType { get; init; }

    [Id(8)]
    public required ImmutableArray<HabbiconShopItemSnapshot> Habbicons { get; init; }
}
