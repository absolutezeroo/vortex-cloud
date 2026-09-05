using System.Collections.Generic;
using System.Collections.Immutable;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Progression;

/// <summary>
/// Folds a track definition together with one player's state into the shape the client reads.
/// </summary>
/// <remarks>
/// Pure, and built on every read rather than stored. <c>available</c> and <c>complete</c> are
/// derived from points and claims; keeping them as columns would be a second copy of a fact the
/// state already carries, and the first content edit would make the two disagree.
/// </remarks>
internal static class TrackViewBuilder
{
    public static RewardTrackViewSnapshot Build(
        RewardTrackDefinitionSnapshot definition,
        PlayerRewardTrackStateSnapshot state
    )
    {
        Dictionary<string, PlayerTaskProgressSnapshot> progressByTask = [];

        foreach (PlayerTaskProgressSnapshot task in state.Tasks)
        {
            progressByTask[task.TaskId] = task;
        }

        HashSet<string> claimed = [.. state.ClaimedPrizeIds];

        List<RewardTrackTaskViewSnapshot> tasks = [];

        foreach (RewardTrackTaskDefinitionSnapshot task in definition.Tasks)
        {
            // A premium-only task is still sent to a free player: the client draws it locked, which
            // is half the reason anyone buys premium. Hiding it would hide the offer.
            tasks.Add(
                new RewardTrackTaskViewSnapshot
                {
                    TaskId = task.TaskId,
                    ActionCode = task.ActionCode,
                    Parameter = task.Parameter,
                    ProgressCount = progressByTask.TryGetValue(
                        task.TaskId,
                        out PlayerTaskProgressSnapshot? progress
                    )
                        ? progress.ProgressCount
                        : 0,
                    Premium = task.Premium,
                    Levels = task.Levels,
                }
            );
        }

        List<RewardTrackPrizeViewSnapshot> prizes = [];
        bool allFreeClaimed = true;
        bool allPremiumClaimed = true;

        foreach (RewardTrackPrizeDefinitionSnapshot prize in definition.Prizes)
        {
            bool isClaimed = claimed.Contains(prize.PrizeId);
            bool available = IsAvailable(prize, state);

            if (prize.Premium)
            {
                allPremiumClaimed &= isClaimed;
            }
            else
            {
                allFreeClaimed &= isClaimed;
            }

            RewardGrantSnapshot? display = prize.Display;

            prizes.Add(
                new RewardTrackPrizeViewSnapshot
                {
                    PrizeId = prize.PrizeId,
                    RequiredPoints = prize.RequiredPoints,
                    Kind = display?.Kind ?? Primitives.RewardTracks.RewardKind.None,
                    RewardTypeId = display?.RewardTypeId ?? string.Empty,
                    ExtraParams = display?.ExtraParams ?? string.Empty,
                    RewardAmount = display?.Amount ?? 0,
                    Premium = prize.Premium,
                    Available = available,
                    Claimed = isClaimed,
                }
            );
        }

        return new RewardTrackViewSnapshot
        {
            TrackId = definition.TrackId,
            Theme = definition.Theme,
            Points = state.Points,
            Premium = definition.Premium,
            PremiumUnlocked = state.PremiumUnlocked,
            // Matching the client's own derivation exactly (RewardTrack.refreshDerivedState): free
            // completion is every non-premium prize claimed, and premium completion is that plus
            // every premium one — or trivially true when the track has no premium tier at all.
            Complete = allFreeClaimed,
            PremiumComplete = definition.Premium is null || (allFreeClaimed && allPremiumClaimed),
            Tasks = [.. tasks],
            Prizes = [.. prizes],
        };
    }

    /// <summary>
    /// Whether a prize is unlocked: enough points, and premium if it needs it. Says nothing about
    /// whether it has been taken — <c>available</c> and <c>claimed</c> are different states, and
    /// the client draws different things for them.
    /// </summary>
    public static bool IsAvailable(
        RewardTrackPrizeDefinitionSnapshot prize,
        PlayerRewardTrackStateSnapshot state
    ) => state.Points >= prize.RequiredPoints && (!prize.Premium || state.PremiumUnlocked);
}
