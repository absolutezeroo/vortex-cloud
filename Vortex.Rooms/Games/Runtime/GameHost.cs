using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Abstractions;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>
/// The runtime's per-game bookkeeping: the module, its phase, the match it is playing and the
/// deadline of a timed phase. Every mutable thing about a game lives here rather than inside the
/// module, which is what makes "a finished match cannot accept score changes" and "events from match
/// N cannot mutate match N+1" enforceable in one place instead of remembered in each game.
/// <para>
/// The three composition properties are set once, during registration, in the order the objects can
/// be built: the arena and the context both need the host, and the module needs the context. Nothing
/// reads them before registration returns.
/// </para>
/// </summary>
internal sealed class GameHost
{
    public IRoomGame Game { get; set; } = null!;

    public RoomGameArena Arena { get; set; } = null!;

    public RoomGameContext Context { get; set; } = null!;

    public GamePhase Phase { get; set; } = GamePhase.Idle;

    public GameMatch? Match { get; set; }

    /// <summary>Per-room match counter. Never reset, so two matches in one room activation always
    /// have different ids.</summary>
    public int Sequence { get; set; }

    /// <summary>When a timed phase (<see cref="GamePhase.Countdown"/>,
    /// <see cref="GamePhase.RoundEnding"/>) is due to advance.</summary>
    public long PhaseDeadlineMs { get; set; }

    public IGameRandom Random { get; set; } = new GameRandom(0);

    /// <summary>Set by a module that has work in flight while it has no match, cleared by the
    /// runtime as it hands over the tick. A module keeps it set for as long as the work lasts.</summary>
    public bool WantsIdleTick { get; set; }

    public bool IsLive => GameStateMachine.IsLive(Phase);
}
