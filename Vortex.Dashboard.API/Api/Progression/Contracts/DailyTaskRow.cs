using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One daily task.
/// </summary>
/// <param name="Completed">Assignments that left Available, which includes the ones not claimed.</param>
/// <param name="CompletionRate">Completed over assigned as a percentage, 0 when never assigned. A
/// task nobody ever completes is the one worth re-tuning.</param>
public sealed record DailyTaskRow(
    int Id,
    string TaskCode,
    string QuestTypeCode,
    bool IsBonus,
    string ImageVersion,
    string CatalogName,
    int RequiredRepeats,
    bool Enabled,
    int SortOrder,
    int Assigned,
    int Completed,
    int Claimed,
    double CompletionRate,
    IReadOnlyList<DailyTaskRewardRow> Rewards
);
