using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The Freeze minigame, as its arena furniture drives it. Kept apart from
/// <see cref="IRoomGameAccess"/> on purpose: both subsystems have a Start and an End, they mean
/// different things, and merging them would collide. Not a grain contract.
/// </summary>
public interface IRoomFreezeAccess
{
    Task StartGameAsync(CancellationToken ct);

    /// <summary>Ends the round and reports the winning team.</summary>
    Task<GameTeamColor> EndGameAsync(CancellationToken ct);

    /// <summary>Throws an ice ball at a tile on the player's behalf.</summary>
    Task ThrowBallAsync(PlayerId playerId, int targetX, int targetY, CancellationToken ct);

    /// <summary>A player stepped onto an ice block.</summary>
    Task OnBlockWalkOnAsync(PlayerId playerId, int x, int y, CancellationToken ct);

    /// <summary>A player stepped onto the exit tile.</summary>
    Task OnExitWalkOnAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>A player stepped onto a team gate.</summary>
    Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct);
}
