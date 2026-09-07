using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One achievement, its ladder rolled up and its progress counted.
/// </summary>
/// <param name="Triggered">Whether anything in the emulator actually advances it. An achievement
/// that is not triggered can still be configured, paid out on paper and reached by nobody, which is
/// the difference this flag exists to show.</param>
/// <param name="FinalRequirement">What the last rung asks for, 0 when the ladder is empty.</param>
/// <param name="PlayersStarted">Players with any progress at all, which is fewer than tracked.</param>
/// <param name="BadgesAwarded">Rungs handed out across every player, not players who finished.</param>
/// <param name="BadgeUrl">The last rung's badge, which is the picture the client puts on the whole
/// achievement. Null for a ladder with no rungs.</param>
public sealed record AchievementListItem(
    int Id,
    string Name,
    string Category,
    int DisplayMethod,
    bool Triggered,
    int LevelCount,
    int TotalScore,
    int CreditsPayout,
    int PointsPayout,
    int FinalRequirement,
    int PlayersTracked,
    int PlayersStarted,
    int PlayersCompleted,
    int BadgesAwarded,
    int HighestLevelReached,
    string? BadgeUrl,
    IReadOnlyList<AchievementLevel> Levels
);
