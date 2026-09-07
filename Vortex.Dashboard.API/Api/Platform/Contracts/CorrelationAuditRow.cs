using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One audited action carrying the correlation id.</summary>
public sealed record CorrelationAuditRow(
    DateTime OccurredAt,
    string Category,
    string Action,
    long? ActorPlayerId,
    string? ActorName
);
