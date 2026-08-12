namespace Vortex.Primitives.Players.Snapshots;

/// <summary>
/// Why a row in the resolution picker cannot be chosen. Not an invention: the client hides the save
/// button for anything non-zero and looks up <c>${resolution.disabled.&lt;state&gt;}</c>, so these
/// two are the only codes the hotel's texts can explain. Adding a third would print the key.
/// </summary>
public enum AchievementResolutionState
{
    /// <summary>Selectable.</summary>
    Selectable = 0,

    /// <summary>"You have already completed all levels in this achievement."</summary>
    AllLevelsCompleted = 1,

    /// <summary>"You already have an unfinished challenge for this achievement."</summary>
    AlreadyChallenged = 2,
}
