namespace Vortex.Primitives.Quests;

/// <summary>
/// Where a player stands on one daily task. The values are the client's own
/// (<c>_SafeCls_2991</c>): it counts <see cref="Completed"/> tasks as the "unseen" badge on the
/// toolbar, and shows the claim button only in that state.
/// </summary>
public enum DailyTaskStatus
{
    /// <summary>Assigned and still being worked on.</summary>
    Available = 0,

    /// <summary>The required repeats are done; the reward is waiting to be claimed.</summary>
    Completed = 1,

    /// <summary>The reward has been handed over.</summary>
    Claimed = 2,
}
