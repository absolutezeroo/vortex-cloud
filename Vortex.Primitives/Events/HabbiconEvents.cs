using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Events;

/// <summary>
/// A player used a Habbicon, after every check passed and the use was actually delivered.
/// </summary>
/// <remarks>
/// Published by the Habbicon domain and consumed by anything that cares — reward-track tasks
/// today, wired triggers when someone builds them. Neither of those is named anywhere in the
/// Habbicon code, which is the whole point: "when a player uses a Habbicon from collection Y"
/// becomes a subscriber, never an edit to the service that raises it.
/// </remarks>
/// <param name="RoomId">The room it was used in, or 0 for a private conversation.</param>
/// <param name="ConversationPlayerId">The other party in a private conversation, or null in a room.</param>
public sealed record HabbiconUsedEvent(
    PlayerId PlayerId,
    int HabbiconId,
    int CollectionId,
    int RoomId,
    PlayerId? ConversationPlayerId
) : IEvent;

/// <summary>A Habbicon entered a player's ownership, whatever the source.</summary>
/// <remarks>Not raised for a repeat grant of something already owned.</remarks>
public sealed record HabbiconGrantedEvent(
    PlayerId PlayerId,
    int HabbiconId,
    int CollectionId,
    HabbiconSource Source
) : IEvent;

/// <summary>
/// A player came to own every ordinary entry of a collection. Raised once, on the grant that
/// completed it — not on every later read.
/// </summary>
public sealed record HabbiconCollectionCompletedEvent(
    PlayerId PlayerId,
    int CollectionId,
    string CollectionCode,
    int RewardHabbiconId
) : IEvent;

/// <summary>A player claimed a completed collection's bonus Habbicon.</summary>
public sealed record HabbiconCollectionRewardClaimedEvent(
    PlayerId PlayerId,
    int CollectionId,
    string CollectionCode,
    int RewardHabbiconId
) : IEvent;
