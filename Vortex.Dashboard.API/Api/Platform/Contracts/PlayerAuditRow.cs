using System;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One audited action this player was either side of.
/// </summary>
/// <param name="Result">The audit result as its stored number. The forensics table does not read
/// it; it is on the wire and stays there until something asks for it by name.</param>
/// <param name="Data">The action-specific payload as the raw JSON string the row stored -- every
/// action writes a different shape into it.</param>
public sealed record PlayerAuditRow(
    DateTime OccurredAt,
    string Category,
    string Action,
    long? ActorPlayerId,
    string? ActorPlayerName,
    long? TargetPlayerId,
    string? TargetPlayerName,
    int? RoomId,
    string? RoomName,
    AuditResult Result,
    string? Data
);
