using System;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One moderation action, with the parts of its payload this page knows how to read already
/// extracted -- the duration and, for a refusal, what was refused.
/// </summary>
/// <param name="Duration">The same duration written for a human, null when there is none.</param>
/// <param name="Reason">Only a refusal carries one; null for every other action.</param>
/// <param name="IsRenewal">True when this pair had already been banned earlier in the window.</param>
public sealed record ModerationRow(
    long Id,
    DateTime OccurredAt,
    string Action,
    string Result,
    long? ActorPlayerId,
    string? ActorName,
    long? TargetPlayerId,
    string? TargetName,
    int? RoomId,
    string? RoomName,
    int? DurationSeconds,
    string? Duration,
    string? Reason,
    bool IsRenewal,
    string? CorrelationId
);
