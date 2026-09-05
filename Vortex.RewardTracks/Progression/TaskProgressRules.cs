using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Progression;

/// <summary>What one progress signal did to one task.</summary>
/// <param name="NewProgress">The task's progress count afterwards.</param>
/// <param name="StagesPaid">
/// The stages that were reached for the first time, in order. Empty when the signal moved the
/// count without finishing anything, which is the common case.
/// </param>
/// <param name="PointsGranted">Track points the stages paid, before any premium boost.</param>
/// <param name="HighestPaidLevelIndex">The new watermark to store.</param>
/// <param name="DistinctKeys">The distinct-key set to store, unchanged for non-distinct tasks.</param>
public readonly record struct TaskProgressOutcome(
    int NewProgress,
    ImmutableArray<int> StagesPaid,
    int PointsGranted,
    int HighestPaidLevelIndex,
    string DistinctKeys
)
{
    /// <summary>Nothing happened: the signal did not apply, or it changed nothing worth storing.</summary>
    public static TaskProgressOutcome None(int progress, int watermark, string distinctKeys) =>
        new(progress, [], 0, watermark, distinctKeys);

    /// <summary>Whether anything needs persisting or telling the client about.</summary>
    public bool Changed(int previousProgress) =>
        NewProgress != previousProgress || !StagesPaid.IsDefaultOrEmpty;
}

/// <summary>
/// How a signal turns into progress, and how progress turns into paid stages.
/// </summary>
/// <remarks>
/// <para>
/// Pure: definition in, stored state in, an outcome out. No clock, no database, no grain. Every
/// rule about double-paying, about which stages a jump past three thresholds pays, and about how
/// each mode counts, lives here and is directly testable — which matters more here than almost
/// anywhere, because the failure mode is silently paying a player twice.
/// </para>
/// <para>
/// There is no switch on a task <em>type</em> anywhere. There are four modes, and a mode decides
/// only how the count moves; everything after that is the same code for every task in the hotel.
/// </para>
/// </remarks>
internal static class TaskProgressRules
{
    private const char DistinctSeparator = '\t';

    /// <summary>
    /// Applies one signal.
    /// </summary>
    /// <param name="amount">
    /// The increment for <see cref="TaskProgressMode.Counter"/>, or the reported total for
    /// <see cref="TaskProgressMode.Absolute"/> and <see cref="TaskProgressMode.Highest"/>. Ignored
    /// by <see cref="TaskProgressMode.Distinct"/>, which counts keys rather than amounts.
    /// </param>
    /// <param name="target">
    /// What the signal was about. Must match the task's <c>Parameter</c> when it has one, and is
    /// the dedup key for a distinct task.
    /// </param>
    /// <param name="premiumUnlocked">
    /// Whether the player holds premium on this track. Premium stages are neither reached nor paid
    /// without it — a free player's progress still climbs past them, so buying premium later pays
    /// what they had already earned.
    /// </param>
    public static TaskProgressOutcome Apply(
        RewardTrackTaskDefinitionSnapshot task,
        int currentProgress,
        int highestPaidLevelIndex,
        string distinctKeys,
        int amount,
        string? target,
        bool premiumUnlocked
    )
    {
        if (task.Levels.IsDefaultOrEmpty)
        {
            return TaskProgressOutcome.None(currentProgress, highestPaidLevelIndex, distinctKeys);
        }

        if (task.Premium && !premiumUnlocked)
        {
            // A premium-only task does not advance at all for a free player. Letting it climb
            // invisibly and pay out the moment premium was bought would be a second, hidden
            // retroactive grant on top of the one that is deliberate.
            return TaskProgressOutcome.None(currentProgress, highestPaidLevelIndex, distinctKeys);
        }

        if (!Matches(task.Parameter, target))
        {
            return TaskProgressOutcome.None(currentProgress, highestPaidLevelIndex, distinctKeys);
        }

        (int newProgress, string newKeys) = Advance(
            task,
            currentProgress,
            distinctKeys,
            amount,
            target
        );

        if (newProgress == currentProgress)
        {
            return TaskProgressOutcome.None(currentProgress, highestPaidLevelIndex, distinctKeys);
        }

        (ImmutableArray<int> paid, int points, int watermark) = PayStages(
            task,
            newProgress,
            highestPaidLevelIndex,
            premiumUnlocked
        );

        return new TaskProgressOutcome(newProgress, paid, points, watermark, newKeys);
    }

    /// <summary>
    /// Writes a progress count directly, as the wired <c>PROGRESS_REWARD_TRACK</c> action does. Pays
    /// whatever stages the new count reaches, by the same watermark rule as everything else.
    /// </summary>
    public static TaskProgressOutcome Set(
        RewardTrackTaskDefinitionSnapshot task,
        int highestPaidLevelIndex,
        string distinctKeys,
        int newProgress,
        bool premiumUnlocked
    )
    {
        if (task.Levels.IsDefaultOrEmpty || (task.Premium && !premiumUnlocked))
        {
            return TaskProgressOutcome.None(newProgress, highestPaidLevelIndex, distinctKeys);
        }

        int clamped = Math.Clamp(newProgress, 0, task.MaxRequiredCount);

        (ImmutableArray<int> paid, int points, int watermark) = PayStages(
            task,
            clamped,
            highestPaidLevelIndex,
            premiumUnlocked
        );

        return new TaskProgressOutcome(clamped, paid, points, watermark, distinctKeys);
    }

    /// <summary>Whether the task has reached its last stage.</summary>
    public static bool IsComplete(RewardTrackTaskDefinitionSnapshot task, int progress) =>
        !task.Levels.IsDefaultOrEmpty && progress >= task.MaxRequiredCount;

    /// <summary>
    /// Moves the count by the mode's own rule, and returns the distinct-key set to store with it.
    /// </summary>
    private static (int Progress, string Keys) Advance(
        RewardTrackTaskDefinitionSnapshot task,
        int currentProgress,
        string distinctKeys,
        int amount,
        string? target
    )
    {
        int max = task.MaxRequiredCount;

        switch (task.Mode)
        {
            case TaskProgressMode.Counter:
                return (Math.Min(max, currentProgress + Math.Max(1, amount)), distinctKeys);

            case TaskProgressMode.Absolute:
                // The world reported a total. It may be lower than last time -- unfriending someone
                // really does take a "have 5 friends" task back down -- and that is the mode's whole
                // point. Stages already paid stay paid; the watermark is what protects them.
                return (Math.Clamp(amount, 0, max), distinctKeys);

            case TaskProgressMode.Highest:
                return (Math.Clamp(Math.Max(currentProgress, amount), 0, max), distinctKeys);

            case TaskProgressMode.Distinct:
                return AdvanceDistinct(currentProgress, distinctKeys, target, max);

            default:
                return (currentProgress, distinctKeys);
        }
    }

    /// <summary>
    /// Counts a key not seen before, once.
    /// </summary>
    /// <remarks>
    /// The set stops growing at the task's own maximum: past that the count cannot move, so there
    /// is nothing left to deduplicate against and no reason to keep remembering. That is what bounds
    /// the column by the content rather than by how long someone plays.
    /// </remarks>
    private static (int Progress, string Keys) AdvanceDistinct(
        int currentProgress,
        string distinctKeys,
        string? target,
        int max
    )
    {
        if (string.IsNullOrEmpty(target) || currentProgress >= max)
        {
            return (currentProgress, distinctKeys);
        }

        foreach (string key in Split(distinctKeys))
        {
            if (string.Equals(key, target, StringComparison.Ordinal))
            {
                return (currentProgress, distinctKeys);
            }
        }

        string next = distinctKeys.Length == 0 ? target : distinctKeys + DistinctSeparator + target;

        return (Math.Min(max, currentProgress + 1), next);
    }

    /// <summary>
    /// Every stage the count now reaches that has not been paid yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The watermark, not the count, decides what is owed. A progress update that jumps past three
    /// thresholds at once pays all three; the same update arriving twice pays nothing the second
    /// time, because the watermark has already moved past them. That single rule is what makes a
    /// reconnect, a duplicated event and a retried grain call all harmless.
    /// </para>
    /// <para>
    /// A premium stage a free player has passed is skipped rather than paid — but the watermark is
    /// <em>not</em> advanced over it, so buying premium later pays it. That is the one place the
    /// engine looks backwards, and it is deliberate: the stage was earned, only the entitlement to
    /// collect it was missing.
    /// </para>
    /// </remarks>
    private static (ImmutableArray<int> Paid, int Points, int Watermark) PayStages(
        RewardTrackTaskDefinitionSnapshot task,
        int progress,
        int highestPaidLevelIndex,
        bool premiumUnlocked
    )
    {
        List<int> paid = [];
        int points = 0;
        int watermark = highestPaidLevelIndex;

        for (int i = 0; i < task.Levels.Length; i++)
        {
            RewardTrackTaskLevelSnapshot level = task.Levels[i];

            if (i <= highestPaidLevelIndex || progress < level.RequiredCount)
            {
                continue;
            }

            if (level.Premium && !premiumUnlocked)
            {
                // Leave the watermark where it is. Advancing it would write off a stage the player
                // reached, and buying premium afterwards would silently skip it.
                break;
            }

            paid.Add(i);
            points += level.PointsReward;
            watermark = i;
        }

        return ([.. paid], points, watermark);
    }

    /// <summary>
    /// Whether a signal's target satisfies a task's parameter. An empty parameter means the task
    /// takes any occurrence, which is what most tasks are.
    /// </summary>
    private static bool Matches(string parameter, string? target) =>
        parameter.Length == 0 || string.Equals(parameter, target, StringComparison.Ordinal);

    private static IEnumerable<string> Split(string keys) =>
        keys.Length == 0
            ? []
            : keys.Split(DistinctSeparator, StringSplitOptions.RemoveEmptyEntries);
}
