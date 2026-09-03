using System;
using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>
/// The runtime's bookkeeping for one ARENA — one installation of one game — not for one game. That
/// is the whole of the difference: a room with two Battle Banzai boards has two of these, each with
/// its own module instance, its own phase, its own match and its own random, so a match on one board
/// cannot see or disturb the other.
/// <para>
/// Every mutable thing about an arena lives here rather than inside the module, which is what makes
/// "a finished match cannot accept score changes" and "events from match N cannot mutate match N+1"
/// enforceable in one place instead of remembered in each game.
/// </para>
/// <para>
/// The composition properties are set once, during registration, in the order the objects can be
/// built: the arena view and the context both need the host, and the module needs the context.
/// Nothing reads them before registration returns.
/// </para>
/// </summary>
internal sealed class ArenaHost
{
    /// <summary>Which installation of which game this is. Set immediately after the module is built,
    /// because the game's id comes from its own profile.</summary>
    public ArenaId Id { get; set; } = ArenaId.None;

    public IRoomGame Game { get; set; } = null!;

    public RoomGameArena View { get; set; } = null!;

    public RoomGameContext Context { get; set; } = null!;

    /// <summary>
    /// The teams and scores this arena plays with. The room's shared book when the game's team space
    /// IS the room's Habbo four — which is every shipped game, so the wired boxes and the coloured
    /// scoreboards keep addressing exactly what they always did — and a private book otherwise,
    /// because a team space the four colours cannot express has nothing to share with them.
    /// </summary>
    public TeamBook Teams
    {
        get =>
            _teams
            ?? throw new InvalidOperationException(
                "An arena's team book is bound AFTER its module is constructed, because which book "
                    + "it gets depends on the teams the module declares. Read _context.Teams where "
                    + "you need it; capturing it in a field initialiser captures nothing."
            );
        set => _teams = value;
    }

    private TeamBook? _teams;

    /// <summary>How this arena's teams map onto the Habbo colours, for the furniture and effects that
    /// can only speak in colours.</summary>
    public HabboTeamPalette Palette { get; set; } = HabboTeamPalette.Standard;

    /// <summary>Whether this arena's scores are the room's Habbo-facing ones. Only a shared-book
    /// arena raises the wired score event and repaints the coloured boards; two sources of truth for
    /// one <c>bb_score_r</c> would only make it flicker.</summary>
    public bool SharesRoomTeams { get; set; }

    public GamePhase Phase { get; set; } = GamePhase.Idle;

    public GameMatch? Match { get; set; }

    /// <summary>Per-arena match counter. Never reset, so two matches on one board always have
    /// different ids.</summary>
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
