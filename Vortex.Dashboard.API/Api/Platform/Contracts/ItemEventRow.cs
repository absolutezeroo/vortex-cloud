using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One thing that happened to an item, with every party named.</summary>
public sealed record ItemEventRow(
    int Id,
    DateTime OccurredAt,
    string EventType,
    long? ActorPlayerId,
    string? ActorPlayerName,
    long? FromOwnerId,
    string? FromOwnerName,
    long? ToOwnerId,
    string? ToOwnerName,
    int? RoomId,
    string? RoomName,
    string? CorrelationId,
    string? Data
);
