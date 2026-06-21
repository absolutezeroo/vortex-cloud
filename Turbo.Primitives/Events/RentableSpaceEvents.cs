using System;

namespace Turbo.Primitives.Events;

// Events for rentable-space furniture. Useful hooks for plugin revenue tracking,
// dashboard occupancy views, and audit logs.

/// <summary>A player started renting a space.</summary>
public sealed record RentalStartedEvent(
    int FurnitureId,
    int RenterId,
    int RoomId,
    int Price,
    string Currency,
    DateTimeOffset RentedUntil
) : IEvent;

/// <summary>A rental expired naturally (timer fired).</summary>
public sealed record RentalExpiredEvent(int FurnitureId, int RenterId, int RoomId) : IEvent;

/// <summary>A rental was cancelled early by the room owner or staff.</summary>
public sealed record RentalCancelledEvent(
    int FurnitureId,
    int RenterId,
    int RoomId,
    int ActorPlayerId
) : IEvent;
