using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The Freeze minigame's own verbs, as its arena furniture drives it — the things only Freeze furniture
/// can ask for. Starting and ending a round is deliberately NOT here: that is the room's game lifecycle,
/// shared with every other game, and it is driven through <see cref="IRoomGameAccess"/> so that whatever
/// starts a round does not have to name the games the room happens to contain. Not a grain contract.
/// </summary>
public interface IRoomFreezeAccess
{
    /// <summary>Throws an ice ball at a tile on the player's behalf.</summary>
    Task ThrowBallAsync(PlayerId playerId, int targetX, int targetY, CancellationToken ct);

    /// <summary>A player stepped onto an ice block.</summary>
    Task OnBlockWalkOnAsync(PlayerId playerId, int x, int y, CancellationToken ct);

    /// <summary>A player stepped onto the exit tile.</summary>
    Task OnExitWalkOnAsync(PlayerId playerId, CancellationToken ct);

    /// <summary>A player stepped onto a team gate.</summary>
    Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct);
}
