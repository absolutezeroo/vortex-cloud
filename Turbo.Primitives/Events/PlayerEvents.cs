using System;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Events;

/// <summary>Internal lifecycle event when a player session is attached to a game session.</summary>
public sealed record PlayerConnectedEvent(PlayerId PlayerId, DateTime ConnectedAtUtc) : IEvent;

/// <summary>Internal lifecycle event when a player session is detached from a game session.</summary>
public sealed record PlayerDisconnectedEvent(
    PlayerId PlayerId,
    DateTime ConnectedAtUtc,
    DateTime DisconnectedAtUtc,
    long SessionDurationSeconds
) : IEvent;

/// <summary>Player entered a room for a tracked user journey.</summary>
public sealed record PlayerEnteredRoomEvent(PlayerId PlayerId, int RoomId, DateTime EnteredAtUtc)
    : IEvent;

/// <summary>Player left a room for a tracked user journey.</summary>
public sealed record PlayerLeftRoomEvent(
    PlayerId PlayerId,
    int RoomId,
    DateTime LeftAtUtc,
    long RoomDurationSeconds
) : IEvent;

/// <summary>Player changed their motto.</summary>
public sealed record PlayerMottoChangedEvent(PlayerId PlayerId, string Motto) : IEvent;

/// <summary>Player changed their avatar figure (look).</summary>
public sealed record PlayerFigureChangedEvent(PlayerId PlayerId, string Figure) : IEvent;
