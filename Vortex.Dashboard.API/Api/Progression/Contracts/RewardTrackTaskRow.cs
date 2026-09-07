using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One task on a track.
/// </summary>
/// <param name="Wired">Whether anything raises this task's action today. A task on an unwired
/// action is configured, visible to players, and can never advance.</param>
/// <param name="Steps">Empty for a plain task: the engine builds its single step from the action
/// above, and the editor pre-fills the same way.</param>
public sealed record RewardTrackTaskRow(
    int Id,
    string TaskId,
    string LocalizationKey,
    string ActionCode,
    bool Wired,
    string Parameter,
    string Mode,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackStepRow> Steps,
    IReadOnlyList<RewardTrackLevelRow> Levels
);
