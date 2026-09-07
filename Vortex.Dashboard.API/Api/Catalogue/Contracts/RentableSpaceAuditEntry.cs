using System;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One rentable-space event.
/// </summary>
/// <param name="Data">The event's payload, still the raw JSON string the audit row stored: each
/// action writes a different shape into it.</param>
public sealed record RentableSpaceAuditEntry(
    int Id,
    DateTime OccurredAt,
    string Action,
    long? ActorPlayerId,
    string? ActorName,
    long? TargetPlayerId,
    string? TargetName,
    int? RoomId,
    long? ItemId,
    string? Data,
    string? CorrelationId
);
