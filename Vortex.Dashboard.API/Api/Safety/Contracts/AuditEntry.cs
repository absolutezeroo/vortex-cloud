using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One audited action: who did what, to whom, where, and how it ended.
/// </summary>
/// <param name="Data">The action-specific payload, kept as the raw JSON string the row stored.
/// Every action writes a different shape into it, so it is parsed by whoever knows which action
/// this is -- typing it as an object here would only be a lie that happens to compile.</param>
/// <remarks>The two names are null when the audited player has since been deleted, which the audit
/// spine deliberately survives.</remarks>
public sealed record AuditEntry(
    int Id,
    DateTime OccurredAt,
    string Category,
    string Action,
    string Severity,
    string Result,
    long? ActorPlayerId,
    string? ActorName,
    long? TargetPlayerId,
    string? TargetName,
    int? RoomId,
    long? ItemId,
    string? IpHash,
    string? CorrelationId,
    string? Data
);
