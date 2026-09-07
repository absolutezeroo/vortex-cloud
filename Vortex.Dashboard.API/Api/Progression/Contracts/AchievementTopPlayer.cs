using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Who is furthest along. The name is null for a deleted player.</summary>
public sealed record AchievementTopPlayer(
    int PlayerId,
    string? PlayerName,
    int Level,
    int Progress,
    DateTime UpdatedAt
);
