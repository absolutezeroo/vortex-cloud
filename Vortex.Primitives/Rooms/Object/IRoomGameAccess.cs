using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The wired team-game subsystem, as game furniture uses it. Not a grain contract -- it runs inside
/// the room's turn. The <c>Task</c>-returning members are genuinely asynchronous underneath (they
/// award badges, persist scores and notify the room); the three reads are not, and are not wrapped.
/// </summary>
public interface IRoomGameAccess
{
    /// <summary>The team this player is on, or <see cref="GameTeamColor.None"/>.</summary>
    GameTeamColor GetTeam(PlayerId playerId);

    int GetTeamScore(GameTeamColor team);

    IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team);

    Task StartGameAsync(CancellationToken ct);

    Task EndGameAsync(CancellationToken ct);

    Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct);

    Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>Scores for a team, capped. False when the cap already stopped it.</summary>
    Task<bool> TryGiveScoreToTeamAsync(
        RoomObjectId box,
        GameTeamColor team,
        int amount,
        int cap,
        CancellationToken ct
    );

    /// <summary>Scores for whichever team the player is on, capped.</summary>
    Task<bool> TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    );
}
