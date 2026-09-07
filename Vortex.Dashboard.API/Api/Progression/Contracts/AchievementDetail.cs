using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One achievement: its ladder, where players sit on it, and who is furthest along.</summary>
public sealed record AchievementDetail(
    int Id,
    string Name,
    string Category,
    int DisplayMethod,
    bool Triggered,
    int LevelCount,
    int CompletedPlayers,
    IReadOnlyList<AchievementLadderRung> Ladder,
    IReadOnlyList<AchievementLevelCount> LevelDistribution,
    IReadOnlyList<AchievementTopPlayer> TopPlayers
);
