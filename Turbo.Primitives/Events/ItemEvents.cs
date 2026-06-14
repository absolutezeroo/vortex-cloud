namespace Turbo.Primitives.Events;

/// <summary>A furniture item came into existence (catalog grant, LTD win, staff grant, ...).
/// <paramref name="Data"/> carries optional source-specific JSON detail.</summary>
public sealed record ItemCreatedEvent(int ItemId, int OwnerId, string? Data) : IEvent;

/// <summary>A furniture item was placed from inventory into a room.</summary>
public sealed record ItemPlacedEvent(
    int ItemId,
    int ActorPlayerId,
    int OwnerId,
    int RoomId,
    string? Data
) : IEvent;

/// <summary>A furniture item was moved within a room.</summary>
public sealed record ItemMovedEvent(int ItemId, int ActorPlayerId, int RoomId, string? Data)
    : IEvent;

/// <summary>A furniture item was picked up from a room back into an inventory.</summary>
public sealed record ItemPickedUpEvent(
    int ItemId,
    int ActorPlayerId,
    int FromOwnerId,
    int ToOwnerId,
    int RoomId
) : IEvent;

/// <summary>
/// A furniture item was permanently destroyed (recycler/ecotron, staff deletion, consumed, ...).
/// No destruction flow exists in the core yet; this event is the ready extension point for one.
/// </summary>
public sealed record ItemDeletedEvent(int ItemId, int OwnerId, int? ActorPlayerId, string? Reason)
    : IEvent;
