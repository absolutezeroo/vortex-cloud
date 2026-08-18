using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The Battle Banzai minigame's own verbs, as its arena furniture drives it — the things only
/// Banzai furniture can ask for. Starting and ending a round is deliberately NOT here: that is the
/// room's shared game lifecycle, driven through <see cref="IRoomGameAccess"/> so that whatever
/// starts a round does not have to name the games the room happens to contain. Not a grain contract.
/// </summary>
public interface IRoomBanzaiAccess
{
    /// <summary>Whether a Banzai round is currently running — the gates read it to go unwalkable.</summary>
    bool IsRoundRunning { get; }

    /// <summary>A player stepped onto an arena tile.</summary>
    Task OnTileWalkOnAsync(PlayerId playerId, int tileIdx, CancellationToken ct);

    /// <summary>A player stepped onto a team gate.</summary>
    Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct);

    /// <summary>A player stepped onto a random teleporter.</summary>
    Task OnTeleportWalkOnAsync(PlayerId playerId, RoomObjectId sourceItemId, CancellationToken ct);
}
