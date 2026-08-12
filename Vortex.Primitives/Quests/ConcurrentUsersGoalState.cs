namespace Vortex.Primitives.Quests;

/// <summary>
/// What the landing-view "players online" widget shows. The values are the client's own, read off
/// <c>ConcurrentUsersInfoElementHandler</c>, which polls the goal every 5 seconds while visible.
/// </summary>
public enum ConcurrentUsersGoalState
{
    /// <summary>No goal configured — the widget hides itself.</summary>
    Disabled = 0,

    /// <summary>The hotel has not reached the target yet; the widget shows the progress.</summary>
    Active = 1,

    /// <summary>The target is met and this player has not claimed the reward yet.</summary>
    Redeem = 2,

    /// <summary>This player already claimed it.</summary>
    Rewarded = 3,
}
