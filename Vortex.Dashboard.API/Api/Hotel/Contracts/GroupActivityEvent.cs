using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One line of the guild audit trail, as the page shows it.
/// </summary>
/// <param name="Data">The event's payload, still the raw JSON string the audit row stored: the
/// shape differs per action, so parsing it belongs to whoever knows which action it is.</param>
public sealed record GroupActivityEvent(
    DateTime OccurredAt,
    string Action,
    int? ActorPlayerId,
    string? ActorPlayerName,
    string Result,
    string? Data
);
