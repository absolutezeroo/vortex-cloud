using System;
using System.Collections.Generic;
using System.Globalization;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Progression;

/// <summary>
/// The facts an unlock condition may need, gathered once by the caller.
/// </summary>
/// <remarks>
/// Passed in rather than fetched so the rule stays pure and so the grain makes each lookup at most
/// once for a whole list of tracks — a hotel with eight chapters would otherwise ask for the same
/// badge list eight times.
/// </remarks>
/// <param name="CompletedTrackIds">Tracks this player has finished.</param>
/// <param name="ClaimedPrizeKeys">Claims as <c>trackId:prizeId</c>.</param>
/// <param name="BadgeCodes">Badges the player holds.</param>
/// <param name="AccountAgeDays">How long the account has existed.</param>
/// <param name="FeatureFlags">Server-config flags, by key.</param>
public readonly record struct UnlockFacts(
    IReadOnlySet<string> CompletedTrackIds,
    IReadOnlySet<string> ClaimedPrizeKeys,
    IReadOnlySet<string> BadgeCodes,
    int AccountAgeDays,
    IReadOnlyDictionary<string, bool> FeatureFlags
);

/// <summary>
/// Who may see a track, and when a track counts as finished.
/// </summary>
/// <remarks>
/// Both are open by extension and closed to campaign names: there is no <c>if (trackId ==
/// "introduction")</c> here or anywhere downstream. A new unlock condition is a new enum member and
/// one arm; a new campaign is rows in a table.
/// </remarks>
internal static class TrackGatingRules
{
    /// <summary>Whether the player has satisfied a track's unlock condition.</summary>
    public static bool IsUnlocked(RewardTrackDefinitionSnapshot track, in UnlockFacts facts) =>
        track.UnlockKind switch
        {
            RewardTrackUnlockKind.Always => true,
            RewardTrackUnlockKind.TrackCompleted => facts.CompletedTrackIds.Contains(
                track.UnlockValue
            ),
            RewardTrackUnlockKind.PrizeClaimed => facts.ClaimedPrizeKeys.Contains(
                track.UnlockValue
            ),
            RewardTrackUnlockKind.BadgeOwned => facts.BadgeCodes.Contains(track.UnlockValue),
            RewardTrackUnlockKind.AccountAgeDays => facts.AccountAgeDays
                >= ParseInt(track.UnlockValue),
            RewardTrackUnlockKind.FeatureFlag => facts.FeatureFlags.TryGetValue(
                track.UnlockValue,
                out bool on
            ) && on,
            // An unlock kind this build does not know is a locked track, never an open one. Content
            // written for a newer server must not fall open on an older one.
            _ => false,
        };

    /// <summary>
    /// Whether a player has met a track's completion policy. Distinct from the two booleans on the
    /// wire, which are the client's own display state: this is what a follow-on chapter unlocks
    /// from and what the completion event fires on.
    /// </summary>
    public static bool IsComplete(
        RewardTrackDefinitionSnapshot track,
        RewardTrackViewSnapshot view,
        PlayerRewardTrackStateSnapshot state
    ) =>
        track.CompletionPolicy switch
        {
            RewardTrackCompletionPolicy.AllFreePrizesClaimed => HasPrizes(track) && view.Complete,
            RewardTrackCompletionPolicy.AllPrizesClaimed => HasPrizes(track)
                && view.Complete
                && view.PremiumComplete,
            RewardTrackCompletionPolicy.MaxPointsReached => HasPrizes(track)
                && state.Points >= MaxRequiredPoints(track),
            RewardTrackCompletionPolicy.AllTasksCompleted => AllTasksComplete(track, state),
            _ => false,
        };

    /// <summary>
    /// A track with no prizes is never complete. Otherwise an empty draft would fire a completion
    /// event for every player who could see it, and unlock every chapter behind it.
    /// </summary>
    private static bool HasPrizes(RewardTrackDefinitionSnapshot track) =>
        !track.Prizes.IsDefaultOrEmpty;

    private static int MaxRequiredPoints(RewardTrackDefinitionSnapshot track)
    {
        int max = 0;

        foreach (RewardTrackPrizeDefinitionSnapshot prize in track.Prizes)
        {
            max = Math.Max(max, prize.RequiredPoints);
        }

        return max;
    }

    private static bool AllTasksComplete(
        RewardTrackDefinitionSnapshot track,
        PlayerRewardTrackStateSnapshot state
    )
    {
        if (track.Tasks.IsDefaultOrEmpty)
        {
            return false;
        }

        Dictionary<string, int> progressByTask = [];

        foreach (PlayerTaskProgressSnapshot progress in state.Tasks)
        {
            progressByTask[progress.TaskId] = progress.ProgressCount;
        }

        foreach (RewardTrackTaskDefinitionSnapshot task in track.Tasks)
        {
            // A premium task a free player cannot reach would make this policy uncompletable for
            // them, so it is not counted against them.
            if (task.Premium && !state.PremiumUnlocked)
            {
                continue;
            }

            if (!TaskProgressRules.IsComplete(task, progressByTask.GetValueOrDefault(task.TaskId)))
            {
                return false;
            }
        }

        return true;
    }

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : int.MaxValue;
}
