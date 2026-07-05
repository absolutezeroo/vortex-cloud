using System;

namespace Turbo.Primitives.Events;

/// <summary>A staff member kicked a player out of a room.</summary>
public sealed record PlayerKickedFromRoomEvent(int ActorPlayerId, int TargetPlayerId, int RoomId)
    : IEvent;

/// <summary>A staff member muted a player inside a room for a bounded duration.</summary>
public sealed record PlayerMutedInRoomEvent(
    int ActorPlayerId,
    int TargetPlayerId,
    int RoomId,
    int DurationSeconds
) : IEvent;

/// <summary>A staff member banned a player from a room for a bounded duration.</summary>
public sealed record PlayerBannedInRoomEvent(
    int ActorPlayerId,
    int TargetPlayerId,
    int RoomId,
    int DurationSeconds
) : IEvent;

/// <summary>A staff member sent a moderation alert/caution to a player.</summary>
public sealed record PlayerAlertedEvent(int ActorPlayerId, int TargetPlayerId, int RoomId) : IEvent;

/// <summary>
/// A moderation action was refused because the actor lacked the required capability. Audited as a
/// denied result so attempted privilege escalation stays visible.
/// </summary>
public sealed record ModerationActionDeniedEvent(
    int ActorPlayerId,
    int TargetPlayerId,
    int RoomId,
    string Action
) : IEvent;

/// <summary>A staff member suspended a player's account (null BannedUntil clears the ban).</summary>
public sealed record PlayerAccountBannedEvent(
    int ActorPlayerId,
    int TargetPlayerId,
    DateTime? BannedUntil,
    string Reason
) : IEvent;

/// <summary>A staff member trading-locked a player's account (null LockedUntil clears the lock).</summary>
public sealed record PlayerTradingLockedEvent(
    int ActorPlayerId,
    int TargetPlayerId,
    DateTime? LockedUntil
) : IEvent;
