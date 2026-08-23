using System;
using System.Linq;
using Vortex.Primitives.Quests.Admin;

namespace Vortex.Progression.Quests;

/// <summary>
/// What the dashboard may save as quest content. Pure, because the mistakes these catch are ones
/// the client never reports: it simply draws a ladder that cannot progress, or a task nobody can
/// finish.
/// </summary>
public static class QuestContentRules
{
    /// <summary>Null when the goal may be saved; otherwise the error code.</summary>
    public static string? ValidateGoal(CommunityGoalSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            return "goal_code_required";
        }

        if (spec.Levels.Count == 0)
        {
            // A goal with no rungs can never progress: the widget would sit at 0% forever.
            return "goal_levels_required";
        }

        if (spec.Levels.Any(l => l.ScoreThreshold < 0))
        {
            return "goal_threshold_negative";
        }

        // Two rungs at the same total cannot both be "next", and the ladder would stall on whichever
        // came first.
        return spec.Levels.Select(l => l.ScoreThreshold).Distinct().Count() == spec.Levels.Count
            ? null
            : "goal_threshold_duplicate";
    }

    /// <summary>Null when the daily task may be saved; otherwise the error code.</summary>
    public static string? ValidateDailyTask(DailyTaskSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.TaskCode))
        {
            return "task_code_required";
        }

        if (string.IsNullOrWhiteSpace(spec.QuestTypeCode))
        {
            // Without an objective nothing advances the task and it can never be completed.
            return "task_quest_type_required";
        }

        if (spec.RequiredRepeats < 1)
        {
            return "task_repeats_invalid";
        }

        if (spec.Rewards.Any(r => string.IsNullOrWhiteSpace(r.RewardTypeId)))
        {
            return "reward_type_required";
        }

        return spec.Rewards.Any(r => r.Amount <= 0) ? "reward_amount_invalid" : null;
    }
}
