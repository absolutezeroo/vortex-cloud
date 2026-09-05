using System;
using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>One player's whole state on one track.</summary>
[GenerateSerializer, Immutable]
public sealed record PlayerRewardTrackStateSnapshot
{
    [Id(0)]
    public required string TrackId { get; init; }

    /// <summary>Track points earned. Never decreases.</summary>
    [Id(1)]
    public required int Points { get; init; }

    [Id(2)]
    public required bool PremiumUnlocked { get; init; }

    [Id(3)]
    public DateTime? PremiumUnlockedAtUtc { get; init; }

    [Id(4)]
    public required ImmutableArray<PlayerTaskProgressSnapshot> Tasks { get; init; }

    /// <summary>Prize ids already claimed.</summary>
    [Id(5)]
    public required ImmutableArray<string> ClaimedPrizeIds { get; init; }

    [Id(6)]
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// The content version this player's row was last reconciled against. When it lags the
    /// definition's, the next push carries the client's <c>reload</c> flag.
    /// </summary>
    [Id(7)]
    public required int ContentVersion { get; init; }
}
