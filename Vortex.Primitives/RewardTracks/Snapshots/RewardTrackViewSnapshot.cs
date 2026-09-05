using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>
/// A track resolved for one player: the definition folded together with their state, in the exact
/// shape the wire wants. Built on read, never stored.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackViewSnapshot
{
    [Id(0)]
    public required string TrackId { get; init; }

    [Id(1)]
    public required string Theme { get; init; }

    [Id(2)]
    public required int Points { get; init; }

    [Id(3)]
    public required RewardTrackPremiumSnapshot? Premium { get; init; }

    [Id(4)]
    public required bool PremiumUnlocked { get; init; }

    /// <summary>Every free prize claimed. The client's <c>complete</c>.</summary>
    [Id(5)]
    public required bool Complete { get; init; }

    /// <summary>Every prize claimed, or the track has no premium tier. The client's <c>premiumComplete</c>.</summary>
    [Id(6)]
    public required bool PremiumComplete { get; init; }

    [Id(7)]
    public required ImmutableArray<RewardTrackTaskViewSnapshot> Tasks { get; init; }

    [Id(8)]
    public required ImmutableArray<RewardTrackPrizeViewSnapshot> Prizes { get; init; }
}
