using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Content;

/// <summary>
/// Checks campaign content for the mistakes that make a track unplayable rather than merely wrong.
/// </summary>
/// <remarks>
/// <para>
/// Pure over a loaded catalog, so it runs at startup, from the dashboard, and in a test with the
/// same code. It reports everything it finds rather than throwing on the first: an operator fixing
/// a campaign wants the list, not one problem at a time.
/// </para>
/// <para>
/// Nothing here is a warning about taste. Every rule below describes content a player would hit —
/// a milestone nobody can reach, a bonus nobody can claim, a chapter that unlocks from itself.
/// </para>
/// </remarks>
internal static class RewardTrackContentValidator
{
    public static RewardTrackContentReport Validate(
        ImmutableArray<RewardTrackDefinitionSnapshot> tracks
    )
    {
        List<RewardTrackContentProblem> problems = [];

        foreach (
            IGrouping<string, RewardTrackDefinitionSnapshot> duplicate in tracks
                .GroupBy(t => t.TrackId, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
        )
        {
            problems.Add(
                new RewardTrackContentProblem(
                    duplicate.Key,
                    "duplicate_track_id",
                    $"{duplicate.Count()} tracks share this id; player rows key on it and would be shared too"
                )
            );
        }

        foreach (RewardTrackDefinitionSnapshot track in tracks)
        {
            ValidateSchedule(track, problems);
            ValidatePremium(track, problems);
            ValidateTasks(track, problems);
            ValidatePrizes(track, problems);
        }

        ValidateUnlockGraph(tracks, problems);

        return new RewardTrackContentReport(problems);
    }

    private static void ValidateSchedule(
        RewardTrackDefinitionSnapshot track,
        List<RewardTrackContentProblem> problems
    )
    {
        if (
            track.StartsAtUtc is { } starts
            && track.ProgressEndsAtUtc is { } progressEnds
            && progressEnds <= starts
        )
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "progress_window_empty",
                    "progress ends before it starts, so no task can ever advance"
                )
            );
        }

        // Claims closing before progress does would strand points a player earned and can never
        // spend — the one scheduling mistake that takes something away from someone.
        if (
            track.ProgressEndsAtUtc is { } ends
            && track.ClaimEndsAtUtc is { } claimEnds
            && claimEnds < ends
        )
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "claims_close_before_progress",
                    "claiming closes before progress does, stranding points nobody can spend"
                )
            );
        }
    }

    private static void ValidatePremium(
        RewardTrackDefinitionSnapshot track,
        List<RewardTrackContentProblem> problems
    )
    {
        bool hasPremiumContent =
            track.Prizes.Any(p => p.Premium)
            || track.Tasks.Any(t => t.Premium)
            || track.Tasks.Any(t => t.Levels.Any(l => l.Premium));

        if (track.Premium is null)
        {
            if (hasPremiumContent)
            {
                // Premium prizes on a track that cannot be upgraded: locked forever, with no button
                // to unlock them.
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "premium_content_without_premium",
                        "has premium tasks or prizes but no premium tier to buy, so they can never unlock"
                    )
                );
            }

            return;
        }

        if (track.Premium.CostCredits <= 0 && track.Premium.CostDiamonds <= 0)
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "premium_free",
                    "premium is priced at nothing; the purchase is refused rather than given away"
                )
            );
        }

        if (track.Premium.BoostPerMille < 1000)
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "premium_boost_below_one",
                    $"boost is {track.Premium.BoostPerMille} per-mille, below 1.0x; it is treated as no boost"
                )
            );
        }

        if (!hasPremiumContent && track.Premium.InstantPoints <= 0)
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "premium_buys_nothing",
                    "premium has no exclusive tasks, prizes or instant points; only the boost would apply"
                )
            );
        }
    }

    private static void ValidateTasks(
        RewardTrackDefinitionSnapshot track,
        List<RewardTrackContentProblem> problems
    )
    {
        foreach (
            IGrouping<string, RewardTrackTaskDefinitionSnapshot> duplicate in track
                .Tasks.GroupBy(t => t.TaskId, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
        )
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "duplicate_task_id",
                    $"task '{duplicate.Key}' appears {duplicate.Count()} times; their progress rows would collide"
                )
            );
        }

        foreach (RewardTrackTaskDefinitionSnapshot task in track.Tasks)
        {
            if (task.Levels.IsDefaultOrEmpty)
            {
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "task_without_levels",
                        $"task '{task.TaskId}' has no stages, so it can never pay anything"
                    )
                );

                continue;
            }

            int previous = 0;

            foreach (RewardTrackTaskLevelSnapshot level in task.Levels)
            {
                if (level.RequiredCount <= 0)
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "level_requires_nothing",
                            $"task '{task.TaskId}' stage {level.LevelIndex} requires {level.RequiredCount}; it would pay the moment the track opens"
                        )
                    );
                }

                if (level.RequiredCount <= previous && level.LevelIndex > 0)
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "levels_not_ascending",
                            $"task '{task.TaskId}' stage {level.LevelIndex} requires {level.RequiredCount}, not more than the previous {previous}"
                        )
                    );
                }

                previous = level.RequiredCount;
            }

            if (task.Mode == TaskProgressMode.Distinct && task.Parameter.Length > 0)
            {
                // A distinct task counts distinct targets; pinning it to one target means it can
                // only ever reach 1.
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "distinct_task_pinned",
                        $"task '{task.TaskId}' counts distinct targets but is pinned to '{task.Parameter}', so it can never exceed 1"
                    )
                );
            }
        }
    }

    private static void ValidatePrizes(
        RewardTrackDefinitionSnapshot track,
        List<RewardTrackContentProblem> problems
    )
    {
        foreach (
            IGrouping<string, RewardTrackPrizeDefinitionSnapshot> duplicate in track
                .Prizes.GroupBy(p => p.PrizeId, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
        )
        {
            problems.Add(
                new RewardTrackContentProblem(
                    track.TrackId,
                    "duplicate_prize_id",
                    $"prize '{duplicate.Key}' appears {duplicate.Count()} times; a claim on one is a claim on both"
                )
            );
        }

        // What the whole track can ever pay, which is what makes a milestone reachable or not. Free
        // players get only the free stages; the premium total is the ceiling with premium bought.
        int freePoints = 0;
        int premiumPoints = 0;

        foreach (RewardTrackTaskDefinitionSnapshot task in track.Tasks)
        {
            foreach (RewardTrackTaskLevelSnapshot level in task.Levels)
            {
                premiumPoints += level.PointsReward;

                if (!task.Premium && !level.Premium)
                {
                    freePoints += level.PointsReward;
                }
            }
        }

        if (track.Premium is { } premium)
        {
            premiumPoints += premium.InstantPoints;
        }

        foreach (RewardTrackPrizeDefinitionSnapshot prize in track.Prizes)
        {
            if (prize.Rewards.IsDefaultOrEmpty)
            {
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "prize_without_rewards",
                        $"prize '{prize.PrizeId}' hands over nothing; claiming it is refused"
                    )
                );
            }

            foreach (RewardGrantSnapshot reward in prize.Rewards)
            {
                if (reward.RewardTypeId.Length == 0)
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "reward_without_target",
                            $"prize '{prize.PrizeId}' has a {reward.Kind} reward naming nothing"
                        )
                    );
                }

                if (RequiresNumericId(reward.Kind) && !IsNumeric(reward.RewardTypeId))
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "reward_target_not_numeric",
                            $"prize '{prize.PrizeId}' has a {reward.Kind} reward naming '{reward.RewardTypeId}', which is not an id"
                        )
                    );
                }

                if (reward.Amount <= 0)
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "reward_amount_not_positive",
                            $"prize '{prize.PrizeId}' has a {reward.Kind} reward with amount {reward.Amount}"
                        )
                    );
                }
            }

            int ceiling = prize.Premium ? premiumPoints : freePoints;

            if (prize.RequiredPoints > ceiling)
            {
                // The one that matters most: a milestone nobody can reach, however long they play.
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "prize_unreachable",
                        $"prize '{prize.PrizeId}' needs {prize.RequiredPoints} points but the track can only ever pay {ceiling}"
                    )
                );
            }
        }
    }

    /// <summary>
    /// Walks the track-completion dependency graph for cycles. Two chapters each waiting on the
    /// other is content that looks fine per track and is unplayable as a set.
    /// </summary>
    private static void ValidateUnlockGraph(
        ImmutableArray<RewardTrackDefinitionSnapshot> tracks,
        List<RewardTrackContentProblem> problems
    )
    {
        Dictionary<string, string> dependsOn = new(StringComparer.Ordinal);
        HashSet<string> known = new(tracks.Select(t => t.TrackId), StringComparer.Ordinal);

        foreach (RewardTrackDefinitionSnapshot track in tracks)
        {
            if (track.UnlockKind == RewardTrackUnlockKind.TrackCompleted)
            {
                if (!known.Contains(track.UnlockValue))
                {
                    problems.Add(
                        new RewardTrackContentProblem(
                            track.TrackId,
                            "unlock_track_missing",
                            $"unlocks after '{track.UnlockValue}', which is not a track"
                        )
                    );

                    continue;
                }

                dependsOn[track.TrackId] = track.UnlockValue;
            }
            else if (
                track.UnlockKind == RewardTrackUnlockKind.AccountAgeDays
                && !IsNumeric(track.UnlockValue)
            )
            {
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "unlock_value_not_numeric",
                        $"unlocks at account age '{track.UnlockValue}', which is not a number of days"
                    )
                );
            }
            else if (
                track.UnlockKind != RewardTrackUnlockKind.Always
                && track.UnlockValue.Length == 0
            )
            {
                problems.Add(
                    new RewardTrackContentProblem(
                        track.TrackId,
                        "unlock_value_missing",
                        $"unlock kind is {track.UnlockKind} but no value is set, so it can never be satisfied"
                    )
                );
            }
        }

        HashSet<string> reported = new(StringComparer.Ordinal);

        foreach (string start in dependsOn.Keys)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            string? current = start;

            while (current is not null && seen.Add(current))
            {
                current = dependsOn.GetValueOrDefault(current);
            }

            if (current is not null && reported.Add(current))
            {
                problems.Add(
                    new RewardTrackContentProblem(
                        current,
                        "unlock_cycle",
                        "is part of a cycle of tracks each waiting on the next; none of them can ever unlock"
                    )
                );
            }
        }
    }

    /// <summary>Kinds whose <c>RewardTypeId</c> the granter parses as a number.</summary>
    private static bool RequiresNumericId(RewardKind kind) =>
        kind
            is RewardKind.WallItem
                or RewardKind.FloorItem
                or RewardKind.AvatarEffect
                or RewardKind.Currency
                or RewardKind.ChatStyle
                or RewardKind.Habbicon;

    private static bool IsNumeric(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
}
