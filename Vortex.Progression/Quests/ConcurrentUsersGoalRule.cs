using Vortex.Primitives.Quests;

namespace Vortex.Players.Quests;

/// <summary>
/// Where a player stands on the hotel-wide "players online" goal. Pure so the four states can be
/// tested without a session gateway or a badge grain.
/// </summary>
public static class ConcurrentUsersGoalRule
{
    /// <summary>
    /// Resolves the widget state. A goal with no configured target counts as disabled — the widget
    /// would otherwise show "0 of 0 players" and offer a reward for logging in at all.
    /// </summary>
    /// <param name="enabled">Whether an operator turned the goal on.</param>
    /// <param name="userCountGoal">Players needed; zero or less disables the goal.</param>
    /// <param name="onlineCount">Players online right now.</param>
    /// <param name="alreadyRewarded">Whether this player already claimed it.</param>
    public static ConcurrentUsersGoalState Resolve(
        bool enabled,
        int userCountGoal,
        int onlineCount,
        bool alreadyRewarded
    )
    {
        if (!enabled || userCountGoal <= 0)
        {
            return ConcurrentUsersGoalState.Disabled;
        }

        // Rewarded outranks the live count: someone who claimed it must not be offered it again the
        // moment the hotel dips below the target and climbs back over.
        if (alreadyRewarded)
        {
            return ConcurrentUsersGoalState.Rewarded;
        }

        return onlineCount >= userCountGoal
            ? ConcurrentUsersGoalState.Redeem
            : ConcurrentUsersGoalState.Active;
    }

    /// <summary>True when a claim should actually grant the reward.</summary>
    public static bool CanClaim(
        bool enabled,
        int userCountGoal,
        int onlineCount,
        bool alreadyRewarded
    ) =>
        Resolve(enabled, userCountGoal, onlineCount, alreadyRewarded)
        == ConcurrentUsersGoalState.Redeem;
}
