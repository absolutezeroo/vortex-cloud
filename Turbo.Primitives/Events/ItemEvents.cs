using System.Collections.Generic;

namespace Turbo.Primitives.Events;

/// <summary>A furniture item came into existence (catalog grant, LTD win, staff grant, ...).
/// <paramref name="Data"/> carries optional source-specific JSON detail.</summary>
public sealed record ItemCreatedEvent(int ItemId, int OwnerId, string? Data) : IEvent;

/// <summary>Two players opened a trade window. The trade-level lifecycle (<c>started</c> →
/// <c>completed</c>/<c>cancelled</c>) is audited under the <c>Item</c> category for dashboard analytics
/// and fraud review; individual item moves are recorded separately as <see cref="ItemTradedEvent"/>.</summary>
public sealed record TradeStartedEvent(int PlayerOneId, int PlayerTwoId, int RoomId) : IEvent;

/// <summary>Two players completed a trade and items changed hands atomically.</summary>
public sealed record TradeCompletedEvent(
    int PlayerOneId,
    int PlayerTwoId,
    IReadOnlyList<int> PlayerOneItemIds,
    IReadOnlyList<int> PlayerTwoItemIds,
    int RoomId
) : IEvent;

/// <summary>A trade ended without an exchange. <paramref name="Reason"/> distinguishes a plain
/// cancellation, a confirmation-time decline, a participant leaving, and a commit-time failure.</summary>
public sealed record TradeCancelledEvent(
    int PlayerOneId,
    int PlayerTwoId,
    int RoomId,
    string Reason
) : IEvent;

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

/// <summary>A furniture item changed owner through a completed player-to-player trade.
/// <paramref name="ActorPlayerId"/> is the trade participant giving the item away.</summary>
public sealed record ItemTradedEvent(
    int ItemId,
    int ActorPlayerId,
    int FromOwnerId,
    int ToOwnerId,
    int RoomId
) : IEvent;

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
