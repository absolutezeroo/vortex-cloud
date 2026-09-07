using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One badge a player holds.
/// </summary>
/// <param name="SlotId">Which of the five slots it is worn in, null when it is only owned.</param>
public sealed record PlayerBadgeRow(
    int Id,
    string BadgeCode,
    string? BadgeUrl,
    int? SlotId,
    DateTime CreatedAt
);
