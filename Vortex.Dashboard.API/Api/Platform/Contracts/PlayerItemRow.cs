using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One item event this player was party to, as actor, giver or receiver.
/// </summary>
/// <remarks>
/// Each of the three party ids carries its resolved name: a trade reads as a sentence only when
/// both ends are named, and that sentence is the line the investigation page shows.
/// </remarks>
public sealed record PlayerItemRow(
    DateTime OccurredAt,
    string EventType,
    long ItemId,
    int? RoomId,
    string? RoomName,
    long? ActorPlayerId,
    string? ActorPlayerName,
    long? FromOwnerId,
    string? FromOwnerName,
    long? ToOwnerId,
    string? ToOwnerName,
    string? CorrelationId,
    string? Data
);
