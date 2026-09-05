using Vortex.Primitives.Players;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Primitives.Events;

/// <summary>A task's stage was reached for the first time, and paid.</summary>
/// <remarks>
/// Raised once per stage per player. Never raised for a stage the player had already been paid
/// for, which is what makes it safe to hang analytics off.
/// </remarks>
public sealed record RewardTrackStageCompletedEvent(
    PlayerId PlayerId,
    string TrackId,
    string TaskId,
    int LevelIndex,
    int PointsGranted
) : IEvent;

/// <summary>A prize was claimed and its whole bundle granted.</summary>
public sealed record RewardTrackPrizeClaimedEvent(
    PlayerId PlayerId,
    string TrackId,
    string PrizeId,
    bool Premium
) : IEvent;

/// <summary>Premium was activated on a track, by purchase or by an operator.</summary>
/// <param name="Purchased">False when an operator granted it rather than the player buying it.</param>
public sealed record RewardTrackPremiumActivatedEvent(
    PlayerId PlayerId,
    string TrackId,
    bool Purchased,
    int CreditsPaid,
    int DiamondsPaid
) : IEvent;

/// <summary>
/// A track met its completion policy. The transition a follow-on chapter unlocks from, and the one
/// worth handing to achievements or analytics.
/// </summary>
public sealed record RewardTrackCompletedEvent(
    PlayerId PlayerId,
    string TrackId,
    RewardTrackCompletionPolicy Policy
) : IEvent;
