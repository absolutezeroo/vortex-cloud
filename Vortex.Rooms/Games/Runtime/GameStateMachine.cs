using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>
/// The one place a game's phase order is written down. Every transition in the system is checked
/// against this table, so an out-of-order start, a double end or a stray callback from a finished
/// match is a rejected, logged no-op instead of a match in an impossible state.
/// <para>
/// It exists because the shape it replaced was four booleans in four files — a coordinator's
/// <c>_isRunning</c>, a Banzai phase enum, a Freeze phase enum and the timer furni's
/// <c>_gameActive</c> — with nothing anywhere saying which combinations were legal.
/// </para>
/// </summary>
public static class GameStateMachine
{
    /// <summary>
    /// <code>
    /// Idle ──► Preparing ──► Countdown ──► Running ──► RoundEnding ──► Finished ──► Resetting ──┐
    ///  ▲            │             │           │             │                          ▲        │
    ///  └────────────┴─────────────┴───────────┴─────────────┴──────────────────────────┘        │
    ///                                         └──────── next round: RoundEnding ──► Preparing   │
    ///  ▲                                                                                        │
    ///  └────────────────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// Every phase can fall to <see cref="GamePhase.Resetting"/>: that is the abort path, taken when
    /// an arena stops validating mid-match, the room unloads, or a module throws its way out of a
    /// phase. Cleanup is never skipped.
    /// </summary>
    public static bool CanTransition(GamePhase from, GamePhase to) =>
        from != to
        && (
            to == GamePhase.Resetting
                ? from != GamePhase.Idle
                : (from, to) switch
                {
                    (GamePhase.Idle, GamePhase.Preparing) => true,
                    (GamePhase.Preparing, GamePhase.Countdown) => true,
                    (GamePhase.Preparing, GamePhase.Running) => true,
                    (GamePhase.Countdown, GamePhase.Running) => true,
                    (GamePhase.Running, GamePhase.RoundEnding) => true,
                    (GamePhase.RoundEnding, GamePhase.Preparing) => true,
                    (GamePhase.RoundEnding, GamePhase.Finished) => true,
                    (GamePhase.Finished, GamePhase.Resetting) => true,
                    (GamePhase.Resetting, GamePhase.Idle) => true,
                    _ => false,
                }
        );

    /// <summary>Whether gameplay signals do anything in this phase. Only one phase says yes, and
    /// every rule that used to ask a boolean asks this instead.</summary>
    public static bool IsLive(GamePhase phase) => phase == GamePhase.Running;

    /// <summary>Whether a match exists at all — anything but the resting state.</summary>
    public static bool HasMatch(GamePhase phase) => phase != GamePhase.Idle;
}
