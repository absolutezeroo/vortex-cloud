using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// The room's game runtime, as furniture and wired boxes use it. Not a grain contract — it runs
/// inside the room's own turn.
/// <para>
/// There is exactly one of these however many games a room hosts. Nothing here names Battle Banzai,
/// Freeze or football: the timer furni starts "the room's games", a wired team condition reads "the
/// room's teams", and arena furniture reports what happened to it as a <see cref="GameSignal"/> that
/// the runtime routes. Adding a game adds no member to this interface — that is the property the old
/// per-game <c>IRoomFreezeAccess</c> / <c>IRoomBanzaiAccess</c> pair did not have.
/// </para>
/// </summary>
public interface IRoomGameAccess
{
    /// <summary>The team this player is on, or <see cref="GameTeamColor.None"/>.</summary>
    GameTeamColor GetTeam(PlayerId playerId);

    int GetTeamScore(GameTeamColor team);

    IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team);

    Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct);

    Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>Scores for a team, capped per score box. False when the cap already stopped it.</summary>
    Task<bool> TryGiveScoreToTeamAsync(
        RoomObjectId box,
        GameTeamColor team,
        int amount,
        int cap,
        CancellationToken ct
    );

    /// <summary>Scores for whichever team the player is on, capped per (box, player).</summary>
    Task<bool> TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    );

    /// <summary>Roots the player where they stand until unlocked — the wired freeze-user box and a
    /// Freeze hit share this one lock, so "frozen" means one thing.</summary>
    void LockMovement(PlayerId playerId);

    /// <summary>Releases a movement lock (wired unfreeze-user, a thaw, a match ending).</summary>
    void UnlockMovement(PlayerId playerId);

    /// <summary>Starts a match in every game whose arena validates. Called by the game-timer furni's
    /// button and by the wired control-clock action; neither of them names a game.</summary>
    Task StartGameAsync(CancellationToken ct);

    /// <summary>Ends every running match.</summary>
    Task EndGameAsync(CancellationToken ct);

    /// <summary>Whether that game has a match in its <see cref="GamePhase.Running"/> phase — what a
    /// team gate reads to go unwalkable mid-match.</summary>
    bool IsRunning(GameId game);

    /// <summary>Routes one component signal to the game that owns the component. The only path from
    /// furniture into game rules.</summary>
    Task SignalAsync(GameSignal signal, CancellationToken ct);
}
