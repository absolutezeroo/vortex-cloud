namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// The explicit lifecycle of one game in one room. Every game is in exactly one of these at any
/// moment; there is no second boolean anywhere that also means "running". The runtime owns the
/// transitions and rejects the ones that are not in <c>GameStateMachine</c>'s table, so an
/// out-of-order start or a double end is a logged no-op rather than a corrupted match.
/// </summary>
public enum GamePhase
{
    /// <summary>No match. Team gates are open and the arena is inert. The resting state.</summary>
    Idle = 0,

    /// <summary>The arena validated and a match context exists; the game resets its furniture and
    /// builds its roster. Entered and left within one room turn.</summary>
    Preparing = 1,

    /// <summary>An optional pre-round hold (<c>GameProfile.CountdownMs</c>, 0 skips it). Players are
    /// locked into their teams but the rules are not live yet.</summary>
    Countdown = 2,

    /// <summary>The rules are live. The only phase in which gameplay signals do anything.</summary>
    Running = 3,

    /// <summary>The round is over and the game is showing the outcome (Banzai's winner flicker, a
    /// football kickoff reset). No further scoring is accepted.</summary>
    RoundEnding = 4,

    /// <summary>Every round is played out and the match result has been published. Terminal for the
    /// match; the runtime moves straight on to <see cref="Resetting"/>.</summary>
    Finished = 5,

    /// <summary>Cleanup: timers cancelled, temporary effects removed, deferred work dropped. Ends at
    /// <see cref="Idle"/> with no state carried across.</summary>
    Resetting = 6,
}
