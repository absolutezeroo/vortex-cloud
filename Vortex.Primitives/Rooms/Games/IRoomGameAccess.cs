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

    /// <summary>
    /// Starts ONE match: the arena this request resolves to. Called by the game-timer furni's button
    /// and by the wired control-clock action — neither of which names a game, and neither of which
    /// has to: <paramref name="source"/> is the furni asking, and the framework resolves the arena
    /// from it (the arena the furni belongs to, else the arena it stands nearest to, else the only
    /// one there is). A room with several arenas and nothing to choose between them starts nothing
    /// and says so, which is the point — this used to start all of them.
    /// </summary>
    /// <param name="source">The furni making the request, or default when there isn't one.</param>
    /// <param name="game">The game to start, when the caller knows it. <see cref="GameId.None"/>
    /// leaves the choice to the resolver.</param>
    /// <returns>Whether a match was actually started.</returns>
    Task<bool> StartGameAsync(RoomObjectId source, GameId game, CancellationToken ct);

    /// <summary>Ends ONE match, resolved the same way — so a counter beside a finished board cannot
    /// reach across the room and stop the match that is still going.</summary>
    Task<bool> EndGameAsync(RoomObjectId source, GameId game, CancellationToken ct);

    /// <summary>Whether that game has a match in its <see cref="GamePhase.Running"/> phase — what a
    /// team gate reads to go unwalkable mid-match.</summary>
    bool IsRunning(GameId game);

    /// <summary>Routes one component signal to the game that owns the component. The only path from
    /// furniture into game rules.</summary>
    Task SignalAsync(GameSignal signal, CancellationToken ct);
}
